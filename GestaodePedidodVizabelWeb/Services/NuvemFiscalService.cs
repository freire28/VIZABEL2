using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Helpers;
using GestaoPedidosVizabel.Models;
using GestaoPedidosVizabel.Models.DTOs.NuvemFiscal;
using Microsoft.EntityFrameworkCore;

namespace GestaoPedidosVizabel.Services
{
    public interface INuvemFiscalService
    {
        Task<string?> ObterTokenAsync
            ();
        Task<CepResponse?> BuscarCepAsync(string cep);
        Task<NFeResponseDTO?> EnviarNFeAsync(long idNfe);
        Task<NFeNuvemFiscalDTO?> ConverterNFeParaDTOAsync(long idNfe);
    }

    public class NuvemFiscalService : INuvemFiscalService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NuvemFiscalService> _logger;
        private string? _tokenCache;
        private DateTime? _tokenExpiresAt;
        private string? _ambienteCache;

        public NuvemFiscalService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<NuvemFiscalService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Obtém o código numérico da UF
        /// </summary>
        private int ObterCodigoUF(string uf)
        {
            return uf?.ToUpper() switch
            {
                "AC" => 12, // Acre
                "AL" => 27, // Alagoas
                "AP" => 16, // Amapá
                "AM" => 13, // Amazonas
                "BA" => 29, // Bahia
                "CE" => 23, // Ceará
                "DF" => 53, // Distrito Federal
                "ES" => 32, // Espírito Santo
                "GO" => 52, // Goiás
                "MA" => 21, // Maranhão
                "MT" => 51, // Mato Grosso
                "MS" => 50, // Mato Grosso do Sul
                "MG" => 31, // Minas Gerais
                "PA" => 15, // Pará
                "PB" => 25, // Paraíba
                "PR" => 41, // Paraná
                "PE" => 26, // Pernambuco
                "PI" => 22, // Piauí
                "RJ" => 33, // Rio de Janeiro
                "RN" => 24, // Rio Grande do Norte
                "RS" => 43, // Rio Grande do Sul
                "RO" => 11, // Rondônia
                "RR" => 14, // Roraima
                "SC" => 42, // Santa Catarina
                "SP" => 35, // São Paulo
                "SE" => 28, // Sergipe
                "TO" => 17, // Tocantins
                _ => 35 // Padrão: São Paulo
            };
        }

        /// <summary>
        /// Calcula o dígito verificador (cDV) da chave de acesso da NFe usando algoritmo módulo 11
        /// </summary>
        /// <param name="chave43Digitos">Os 43 dígitos da chave de acesso (sem o dígito verificador)</param>
        /// <returns>O dígito verificador (0-9)</returns>
        private int CalcularDigitoVerificador(string chave43Digitos)
        {
            if (string.IsNullOrEmpty(chave43Digitos) || chave43Digitos.Length != 43)
            {
                throw new ArgumentException("A chave deve ter exatamente 43 dígitos", nameof(chave43Digitos));
            }

            int soma = 0;
            int peso = 2;

            // Percorrer os dígitos da direita para a esquerda
            for (int i = chave43Digitos.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(chave43Digitos[i]))
                {
                    int digito = int.Parse(chave43Digitos[i].ToString());
                    soma += digito * peso;
                    peso++;
                    if (peso > 9)
                    {
                        peso = 2;
                    }
                }
            }

            int resto = soma % 11;
            
            // Se o resto for 0 ou 1, o dígito verificador é 0
            // Caso contrário, o dígito verificador é 11 - resto
            if (resto == 0 || resto == 1)
            {
                return 0;
            }
            else
            {
                return 11 - resto;
            }
        }

        /// <summary>
        /// Obtém o ambiente configurado (0 = sandbox, 1 = produção)
        /// </summary>
        private async Task<int> ObterAmbienteAsync()
        {
            var ambienteStr = await ConfiguracaoHelper.BuscarValorConfiguracaoAsync(_context, "AMBIENTE_NFE");
            
            // Se o ambiente mudou, limpar cache do token
            if (!string.IsNullOrEmpty(_ambienteCache) && _ambienteCache != ambienteStr)
            {
                _tokenCache = null;
                _tokenExpiresAt = null;
                _logger.LogInformation("Ambiente alterado, cache do token limpo");
            }
            
            _ambienteCache = ambienteStr;

            if (int.TryParse(ambienteStr, out var ambienteValue))
            {
                return ambienteValue;
            }

            // Padrão: sandbox (0)
            _logger.LogWarning("AMBIENTE_NFE não configurado ou inválido, usando sandbox (0) como padrão");
            return 0;
        }

        /// <summary>
        /// Obtém a URL base da API conforme o ambiente
        /// </summary>
        private async Task<string> ObterUrlBaseApiAsync()
        {
            var ambiente = await ObterAmbienteAsync();
            
            // 0 = sandbox, 1 = produção
            return ambiente == 1 
                ? "https://api.nuvemfiscal.com.br" 
                : "https://api.sandbox.nuvemfiscal.com.br";
        }

        /// <summary>
        /// Obtém a URL de autenticação (mesma para ambos os ambientes)
        /// </summary>
        private string ObterUrlAuth()
        {
            // URL de autenticação é a mesma para sandbox e produção
            return "https://auth.nuvemfiscal.com.br/oauth/token";        
        }

        public async Task<string?> ObterTokenAsync()
        {
            // Verificar se o token ainda é válido (com margem de 5 minutos)
            if (!string.IsNullOrEmpty(_tokenCache) && 
                _tokenExpiresAt.HasValue && 
                _tokenExpiresAt.Value > DateTime.UtcNow.AddMinutes(5))
            {
                return _tokenCache;
            }

            try
            {
                var clienteId = await ConfiguracaoHelper.BuscarValorConfiguracaoAsync(_context, "CLIENTE_ID");
                var clienteSecret = await ConfiguracaoHelper.BuscarValorConfiguracaoAsync(_context, "CLIENTE_SECRET");

                if (string.IsNullOrWhiteSpace(clienteId) || string.IsNullOrWhiteSpace(clienteSecret))
                {
                    _logger.LogError("CLIENTE_ID ou CLIENTE_SECRET não configurados na tabela Configuracoes");
                    return null;
                }

                var httpClient = _httpClientFactory.CreateClient();
                
                // Endpoint de autenticação (mesmo para ambos os ambientes)
                var tokenUrl = ObterUrlAuth();
                _logger.LogInformation($"Usando URL de autenticação: {tokenUrl}");
                                
                // Formato application/x-www-form-urlencoded
                var requestBody = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clienteId),
                    new KeyValuePair<string, string>("client_secret", clienteSecret),
                    new KeyValuePair<string, string>("scope", "empresa cep cnpj nfe")
                };

                var content = new FormUrlEncodedContent(requestBody);
                var response = await httpClient.PostAsync(tokenUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Resposta do token: {responseContent}");
                    
                    try
                    {
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, jsonOptions);

                        if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                        {
                            _tokenCache = tokenResponse.AccessToken;
                            // Assumir que o token expira em 1 hora (ajustar conforme resposta da API)
                            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300); // 5 minutos de margem
                            return _tokenCache;
                        }
                        else
                        {
                            // Tentar deserializar manualmente usando JsonDocument
                            using var doc = JsonDocument.Parse(responseContent);
                            var root = doc.RootElement;
                            
                            string? accessToken = null;
                            
                            // Tentar diferentes nomes de propriedade
                            if (root.TryGetProperty("access_token", out var accessTokenElement))
                                accessToken = accessTokenElement.GetString();
                            else if (root.TryGetProperty("accessToken", out var accessTokenElement2))
                                accessToken = accessTokenElement2.GetString();
                            else if (root.TryGetProperty("token", out var tokenElement))
                                accessToken = tokenElement.GetString();
                            
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                _tokenCache = accessToken;
                                
                                // Tentar obter expires_in
                                int expiresIn = 3600; // padrão 1 hora
                                if (root.TryGetProperty("expires_in", out var expiresInElement))
                                    expiresIn = expiresInElement.GetInt32();
                                else if (root.TryGetProperty("expiresIn", out var expiresInElement2))
                                    expiresIn = expiresInElement2.GetInt32();
                                
                                _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 300); // 5 minutos de margem
                                return _tokenCache;
                            }
                            
                            _logger.LogError($"Token não encontrado na resposta. Resposta completa: {responseContent}");
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, $"Erro ao deserializar resposta do token. Resposta: {responseContent}");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Erro ao obter token da Nuvem Fiscal. Status: {response.StatusCode}, Response: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter token da Nuvem Fiscal");
            }

            return null;
        }

        public async Task<CepResponse?> BuscarCepAsync(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
                return null;

            // Remover caracteres não numéricos do CEP
            cep = new string(cep.Where(char.IsDigit).ToArray());

            if (cep.Length != 8)
            {
                _logger.LogWarning($"CEP inválido: {cep}");
                return null;
            }

            try
            {
                var token = await ObterTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Não foi possível obter token da Nuvem Fiscal");
                    return null;
                }

                var httpClient = _httpClientFactory.CreateClient();
                
                // URL base conforme ambiente
                var urlBase = await ObterUrlBaseApiAsync();
                httpClient.BaseAddress = new Uri($"{urlBase}/");
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                _logger.LogInformation($"Buscando CEP {cep} na URL: {urlBase}/cep/{cep}");

                var response = await httpClient.GetAsync($"cep/{cep}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Tentar deserializar com diferentes formatos
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var cepData = JsonSerializer.Deserialize<CepResponse>(responseContent, jsonOptions);
                    
                    // Mapear campos da API para o modelo usando JsonDocument para garantir mapeamento correto
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;
                    
                    cepData ??= new CepResponse();
                    
                    // Mapear campos da API
                    if (root.TryGetProperty("bairro", out var bairro))
                        cepData.Bairro = bairro.GetString();
                    
                    if (root.TryGetProperty("cep", out var cepValue))
                        cepData.Cep = cepValue.GetString();
                    
                    if (root.TryGetProperty("codigo_ibge", out var codigoIbge))
                        cepData.Ibge = codigoIbge.GetString();
                    else if (root.TryGetProperty("ibge", out var ibge))
                        cepData.Ibge = ibge.GetString();
                    
                    if (root.TryGetProperty("complemento", out var complemento))
                        cepData.Complemento = complemento.GetString();
                    
                    // Concatenar tipo_logradouro com logradouro se existir
                    var tipoLogradouro = root.TryGetProperty("tipo_logradouro", out var tipoLog) ? tipoLog.GetString() : null;
                    var logradouro = root.TryGetProperty("logradouro", out var log) ? log.GetString() : null;
                    
                    if (!string.IsNullOrEmpty(tipoLogradouro) && !string.IsNullOrEmpty(logradouro))
                        cepData.Logradouro = $"{tipoLogradouro} {logradouro}".Trim();
                    else if (!string.IsNullOrEmpty(logradouro))
                        cepData.Logradouro = logradouro;
                    else if (!string.IsNullOrEmpty(tipoLogradouro))
                        cepData.Logradouro = tipoLogradouro;
                    
                    if (root.TryGetProperty("municipio", out var municipio))
                        cepData.Cidade = municipio.GetString();
                    else if (root.TryGetProperty("localidade", out var localidade))
                        cepData.Cidade = localidade.GetString();
                    else if (root.TryGetProperty("cidade", out var cidade))
                        cepData.Cidade = cidade.GetString();
                    
                    if (root.TryGetProperty("uf", out var uf))
                        cepData.Uf = uf.GetString();

                    return cepData;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Erro ao buscar CEP na Nuvem Fiscal. Status: {response.StatusCode}, Response: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar CEP {cep} na Nuvem Fiscal");
            }

            return null;
        }

        /// <summary>
        /// Converte uma NFe do banco de dados para o formato DTO da API NuvemFiscal
        /// Considerando regime Simples Nacional
        /// Formato esperado: { "infNFe": {...}, "infNFeSupl": {...}, "ambiente": "...", "referencia": "..." }
        /// </summary>
        public async Task<NFeNuvemFiscalDTO?> ConverterNFeParaDTOAsync(long idNfe)
        {
            try
            {
                var nfe = await _context.NFes
                    .Include(n => n.Destinatario)
                    .Include(n => n.Itens)
                        .ThenInclude(i => i.Imposto)
                    .Include(n => n.Pagamentos)
                    .FirstOrDefaultAsync(n => n.IdNfe == idNfe);

                if (nfe == null)
                {
                    _logger.LogError($"NFe {idNfe} não encontrada");
                    return null;
                }

                if (nfe.Destinatario == null)
                {
                    _logger.LogError($"NFe {idNfe} não possui destinatário");
                    return null;
                }

                var ambiente = await ObterAmbienteAsync();
                var ambienteStr = ambiente == 1 ? "producao" : "homologacao";
                var tpAmb = ambiente == 1 ? 1 : 2; // 1=Produção, 2=Homologação

                // Buscar dados do emitente (empresa) da tabela EMPRESA
                var empresa = await _context.Empresas
                    .FirstOrDefaultAsync(e => e.IdEmpresa == nfe.IdEmpresa);

                if (empresa == null)
                {
                    _logger.LogError($"Empresa {nfe.IdEmpresa} não encontrada para NFe {idNfe}");
                    return null;
                }

                // Determinar CRT (Código de Regime Tributário) - usar da empresa ou padrão
                var crt = empresa.Crt ?? 1; // Padrão: Simples Nacional

                var isSimplesNacional = crt == 1 || crt == 2;

                // Obter código da UF baseado no endereço da empresa
                // Mapear UF para código (ex: SP = 35, RJ = 33, etc.)
                var cUF = ObterCodigoUF(empresa.EnderecoUf ?? "SP");

                // Gerar código aleatório para cNF (8 dígitos)
                var random = new Random();
                var cNF = random.Next(10000000, 99999999).ToString();

                // Determinar idDest (1=Operação interna, 2=Operação interestadual, 3=Operação com exterior)
                var idDest = empresa.EnderecoUf?.ToUpper() == nfe.Destinatario.Uf?.ToUpper() ? 1 : 2;

                // Gerar Id da NFe no formato: NFe + 44 dígitos
                // Formato: NFe + cUF (2) + AAMM (4) + CNPJ (14) + mod (2) + serie (3) + nNF (9) + tpEmis (1) + cNF (8) + cDV (1) = 44 dígitos
                var cnpjEmpresa = empresa.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", "").Trim() ?? "";
                var aamm = nfe.DataEmissao.ToString("yyMM");
                var mod = "55"; // NFe
                var serieStr = nfe.Serie.ToString().PadLeft(3, '0');
                var nNFStr = nfe.Numero.ToString().PadLeft(9, '0');
                var tpEmis = "1"; // Emissão normal
                var cNFStr = cNF.PadLeft(8, '0');
                
                // Montar os 43 primeiros dígitos (sem o dígito verificador)
                // cUF (2) + AAMM (4) + CNPJ (14) + mod (2) + serie (3) + nNF (9) + tpEmis (1) + cNF (8) = 43 dígitos
                var chave43Digitos = $"{cUF:D2}{aamm}{cnpjEmpresa.PadLeft(14, '0')}{mod}{serieStr}{nNFStr}{tpEmis}{cNFStr}";
                
                // Garantir exatamente 43 dígitos antes de calcular o dígito verificador
                if (chave43Digitos.Length < 43)
                {
                    chave43Digitos = chave43Digitos.PadRight(43, '0');
                }
                else if (chave43Digitos.Length > 43)
                {
                    chave43Digitos = chave43Digitos.Substring(0, 43);
                }
                
                // Calcular dígito verificador (cDV) usando algoritmo módulo 11
                var cDV = CalcularDigitoVerificador(chave43Digitos);
                
                // Montar Id completo com 44 dígitos (43 + dígito verificador)
                // Nota: O Id não é enviado no JSON, a API NuvemFiscal gera automaticamente
                var digitosId = $"{chave43Digitos}{cDV}";
                var idNFe = $"NFe{digitosId}";

                // Criar DTO principal
                var dto = new NFeNuvemFiscalDTO
                {
                    Ambiente = ambienteStr,
                    Referencia = $"NFe-{nfe.IdNfe}",
                    InfNFe = new InfNFeDTO
                    {
                        Versao = "4.00",
                        // Id não é enviado - a API gera automaticamente baseado nos dados do ide
                        Ide = new IdeDTO
                        {
                            CUF = cUF,
                            CNF = cNF,
                            NatOp = nfe.NaturezaOperacao,
                            Mod = 55, // 55 = NFe
                            Serie = nfe.Serie,
                            NNF = nfe.Numero,
                            // Formato de data/hora em UTC (ISO 8601)
                            DhEmi = nfe.DataEmissao.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            DhSaiEnt = nfe.DataSaida?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            TpNF = nfe.TipoNfe, // 0=Entrada, 1=Saída
                            IdDest = idDest,
                            CMunFG = empresa.EnderecoCodMunicipio?.ToString().PadLeft(7, '0') ?? "0000000",
                            TpImp = 1, // 1=Retrato
                            TpEmis = 1, // 1=Emissão normal
                            CDV =  null ,//cDV,
                            TpAmb = tpAmb,
                            FinNFe = nfe.Finalidade, // 1=Normal, 2=Complementar, 3=Ajuste, 4=Devolução
                            IndFinal = 1, // 0=Não, 1=Sim (consumidor final) - ajustar conforme necessário
                            IndPres = 1, // 1=Operação presencial - ajustar conforme necessário
                            ProcEmi = 0, // 0=Emissão de NF-e com aplicativo do contribuinte
                            VerProc = "GestaoPedidosVizabel"
                        }
                    }
                };

                // Emitente
                if (!string.IsNullOrEmpty(empresa.Cnpj))
                {
                    var cnpjLimpo = empresa.Cnpj.Replace(".", "").Replace("/", "").Replace("-", "").Trim();
                    dto.InfNFe.Emit = new EmitDTO
                    {
                        CNPJ = cnpjLimpo,
                        XNome = empresa.RazaoSocial,
                        XFant = empresa.Fantasia,
                        IE = empresa.InscricaoEstadual,
                        IM = empresa.InscricaoMunicipal,
                        CRT = crt,
                        EnderEmit = new EnderEmitDTO
                        {
                            XLgr = empresa.EnderecoLogradouro ?? "NÃO INFORMADO",
                            Nro = empresa.EnderecoNumero ?? "S/N",
                            XBairro = empresa.EnderecoBairro ?? "NÃO INFORMADO",
                            CMun = empresa.EnderecoCodMunicipio?.ToString().PadLeft(7, '0') ?? "0000000",
                            XMun = empresa.EnderecoCidade ?? "NÃO INFORMADO",
                            UF = empresa.EnderecoUf ?? "XX",
                            CEP = empresa.EnderecoCep?.Replace("-", "").Trim() ?? "00000000",
                            CPais = "1058", // Brasil
                            XPais = "BRASIL",
                            Fone = empresa.Fone
                        }
                    };
                }
                else
                {
                    _logger.LogError($"Empresa {empresa.IdEmpresa} não possui CNPJ cadastrado");
                    return null;
                }

                // Destinatário
                var cnpjCpf = nfe.Destinatario.CnpjCpf?.Replace(".", "").Replace("/", "").Replace("-", "").Trim() ?? "";
                
                // Validar e determinar se é CNPJ ou CPF
                var isCnpj = cnpjCpf.Length == 14;
                var isCpf = cnpjCpf.Length == 11;
                
                // Se não tiver CNPJ/CPF válido, usar um valor padrão (pode causar erro, mas melhor que null)
                if (!isCnpj && !isCpf)
                {
                    _logger.LogWarning($"NFe {idNfe}: CNPJ/CPF do destinatário inválido (tamanho: {cnpjCpf.Length}). Usando valor padrão.");
                    // Tentar usar como CNPJ se tiver pelo menos 11 dígitos, senão usar CPF
                    if (cnpjCpf.Length >= 11)
                    {
                        cnpjCpf = cnpjCpf.PadLeft(14, '0').Substring(0, 14);
                        isCnpj = true;
                    }
                    else
                    {
                        cnpjCpf = cnpjCpf.PadLeft(11, '0').Substring(0, 11);
                        isCpf = true;
                    }
                }

                dto.InfNFe.Dest = new DestDTO
                {
                    XNome = nfe.Destinatario.Nome,
                    IndIEDest = nfe.Destinatario.IndIeDest, // 1=Contribuinte, 2=Isento, 9=Não contribuinte
                    IE = nfe.Destinatario.IndIeDest == 1 ? nfe.Destinatario.Ie : null,
                    EnderDest = new EnderDestDTO
                    {
                        XLgr = nfe.Destinatario.Logradouro,
                        Nro = nfe.Destinatario.Numero,
                        XBairro = nfe.Destinatario.Bairro,
                        CMun = nfe.Destinatario.CodMun.ToString().PadLeft(7, '0'),
                        XMun = nfe.Destinatario.Municipio,
                        UF = nfe.Destinatario.Uf,
                        CEP = nfe.Destinatario.Cep?.Replace("-", "").Trim() ?? "00000000",
                        CPais = "1058", // Brasil
                        XPais = "BRASIL"
                    }
                };

                // Preencher CNPJ ou CPF (obrigatório ter um deles)
                if (isCnpj)
                {
                    dto.InfNFe.Dest.CNPJ = cnpjCpf;
                }
                else if (isCpf)
                {
                    dto.InfNFe.Dest.CPF = cnpjCpf;
                }
                else
                {
                    // Fallback: usar CNPJ genérico (pode causar erro, mas melhor que null)
                    _logger.LogError($"NFe {idNfe}: Não foi possível determinar CNPJ ou CPF do destinatário. Usando CNPJ genérico.");
                    dto.InfNFe.Dest.CNPJ = "00000000000000";
                }

                // Itens (det)
                int itemNumero = 1;
                foreach (var item in nfe.Itens.OrderBy(i => i.IdItem))
                {
                    var detDto = new DetDTO
                    {
                        NItem = itemNumero++,
                        Prod = new ProdDTO
                        {
                            CProd = item.CodProduto,
                            CEAN = "SEM GTIN", // Obrigatório - usar "SEM GTIN" quando não houver código de barras
                            XProd = item.Descricao,
                            NCM = item.Ncm,
                            CFOP = item.Cfop,
                            UCom = item.Unidade,
                            QCom = item.Quantidade,
                            VUnCom = item.ValorUnitario,
                            VProd = item.ValorTotal,
                            CEANTrib = "SEM GTIN", // Obrigatório - usar "SEM GTIN" quando não houver código de barras tributário
                            UTrib = item.Unidade,
                            QTrib = item.Quantidade,
                            VUnTrib = item.ValorUnitario,
                            IndTot = 1 // 1=Valor do item compõe o valor total da NF-e
                        }
                    };

                    // Impostos
                    if (item.Imposto != null)
                    {
                        detDto.Imposto = new ImpostoDTO
                        {
                            ICMS = new ICMSDTO()
                        };

                        // ICMS - Sempre usar ICMSSN102 para Simples Nacional
                        if (isSimplesNacional)
                        {
                            detDto.Imposto.ICMS.ICMSSN102 = new ICMSSN102DTO
                            {
                                Orig = item.Imposto.Origem >= 0 && item.Imposto.Origem <= 8 ? item.Imposto.Origem : 0, // 0=Nacional (padrão se inválido)
                                CSOSN = "102"
                            };
                        }
                        else
                        {
                            // Regime Normal - usar CST
                            var cstCsosn = item.Imposto.CstCsosn ?? "90"; // Padrão para Regime Normal
                            switch (cstCsosn)
                            {
                                case "00":
                                    detDto.Imposto.ICMS.ICMS00 = new ICMS00DTO
                                    {
                                        Orig = item.Imposto.Origem,
                                        CST = "00",
                                        ModBC = 0, // Ajustar conforme necessário
                                        VBC = item.Imposto.BaseIcms,
                                        PICMS = item.Imposto.AliquotaIcms,
                                        VICMS = item.Imposto.ValorIcms
                                    };
                                    break;
                                case "20":
                                    detDto.Imposto.ICMS.ICMS20 = new ICMS20DTO
                                    {
                                        Orig = item.Imposto.Origem,
                                        CST = "20",
                                        ModBC = 0,
                                        VBC = item.Imposto.BaseIcms,
                                        PICMS = item.Imposto.AliquotaIcms,
                                        VICMS = item.Imposto.ValorIcms
                                    };
                                    break;
                                case "40":
                                    detDto.Imposto.ICMS.ICMS40 = new ICMS40DTO
                                    {
                                        Orig = item.Imposto.Origem,
                                        CST = "40"
                                    };
                                    break;
                                case "90":
                                default:
                                    detDto.Imposto.ICMS.ICMS90 = new ICMS90DTO
                                    {
                                        Orig = item.Imposto.Origem,
                                        CST = "90",
                                        VBC = item.Imposto.BaseIcms > 0 ? item.Imposto.BaseIcms : null,
                                        PICMS = item.Imposto.AliquotaIcms > 0 ? item.Imposto.AliquotaIcms : null,
                                        VICMS = item.Imposto.ValorIcms > 0 ? item.Imposto.ValorIcms : null
                                    };
                                    break;
                            }
                        }

                        // PIS
                        if (item.Imposto.ValorPis.HasValue && item.Imposto.ValorPis.Value > 0)
                        {
                            var basePis = item.Imposto.BasePis ?? item.ValorTotal;
                            var aliquotaPis = basePis > 0 ? (item.Imposto.ValorPis.Value / basePis) * 100 : 0;
                            
                            detDto.Imposto.PIS = new PISDTO
                            {
                                PISAliq = new PISAliqDTO
                                {
                                    CST = "01",
                                    VBC = basePis,
                                    PPIS = aliquotaPis,
                                    VPIS = item.Imposto.ValorPis.Value
                                }
                            };
                        }
                        else
                        {
                            detDto.Imposto.PIS = new PISDTO
                            {
                                PISNT = new PISNTDTO { CST = "04" }
                            };
                        }

                        // COFINS
                        if (item.Imposto.ValorCofins.HasValue && item.Imposto.ValorCofins.Value > 0)
                        {
                            var baseCofins = item.Imposto.BaseCofins ?? item.ValorTotal;
                            var aliquotaCofins = baseCofins > 0 ? (item.Imposto.ValorCofins.Value / baseCofins) * 100 : 0;
                            
                            detDto.Imposto.COFINS = new COFINSDTO
                            {
                                COFINSAliq = new COFINSAliqDTO
                                {
                                    CST = "01",
                                    VBC = baseCofins,
                                    PCOFINS = aliquotaCofins,
                                    VCOFINS = item.Imposto.ValorCofins.Value
                                }
                            };
                        }
                        else
                        {
                            detDto.Imposto.COFINS = new COFINSDTO
                            {
                                COFINSNT = new COFINSNTDTO { CST = "04" }
                            };
                        }
                    }
                    else
                    {
                        // Sem impostos - usar valores padrão para Simples Nacional
                        // Determinar origem baseado no regime tributário (0=Nacional é o padrão mais comum)
                        var origemPadrao = 0; // 0=Nacional
                        
                        detDto.Imposto = new ImpostoDTO
                        {
                            ICMS = new ICMSDTO
                            {
                                ICMSSN102 = new ICMSSN102DTO
                                {
                                    Orig = origemPadrao, // 0=Nacional
                                    CSOSN = "102"
                                }
                            },
                            PIS = new PISDTO
                            {
                                PISNT = new PISNTDTO { CST = "04" }
                            },
                            COFINS = new COFINSDTO
                            {
                                COFINSNT = new COFINSNTDTO { CST = "04" }
                            }
                        };
                    }

                    dto.InfNFe.Det.Add(detDto);
                }

                // Totais - Todos os campos são obrigatórios, mesmo que sejam zero
                var totalIcms = nfe.Itens.Sum(i => i.Imposto?.ValorIcms ?? 0);
                var totalPis = nfe.Itens.Sum(i => i.Imposto?.ValorPis ?? 0);
                var totalCofins = nfe.Itens.Sum(i => i.Imposto?.ValorCofins ?? 0);
                var totalBaseIcms = nfe.Itens.Sum(i => i.Imposto?.BaseIcms ?? 0);
                // BaseIcmsSt e ValorIcmsSt não existem no modelo, usar 0
                var totalBaseIcmsSt = 0;
                var totalIcmsSt = 0;

                dto.InfNFe.Total = new TotalDTO
                {
                    ICMSTot = new ICMSTotDTO
                    {
                        VBC = totalBaseIcms, // Obrigatório
                        VICMS = totalIcms, // Obrigatório
                        VICMSDeson = 0, // Obrigatório
                        VFCP = 0, // Obrigatório
                        VBCST = totalBaseIcmsSt, // Obrigatório
                        VST = totalIcmsSt, // Obrigatório
                        VFCPST = 0, // Obrigatório
                        VFCPSTRet = 0, // Obrigatório
                        VProd = nfe.ValorProdutos,
                        VFrete = 0, // Obrigatório
                        VSeg = 0, // Obrigatório
                        VDesc = 0, // Obrigatório
                        VII = 0, // Obrigatório
                        VIPI = 0, // Obrigatório
                        VIPIDevol = 0, // Obrigatório
                        VPIS = totalPis, // Obrigatório
                        VCOFINS = totalCofins, // Obrigatório
                        VOutro = 0, // Obrigatório
                        VNF = nfe.ValorTotalNfe,
                        VTotTrib = (totalIcms + totalPis + totalCofins) > 0 ? (totalIcms + totalPis + totalCofins) : 0
                    }
                };

                // Transporte
                dto.InfNFe.Transp = new TranspDTO
                {
                    ModFrete = 0 // 0=Por conta do emitente
                };

                // Pagamento - Campo obrigatório
                if (nfe.Pagamentos != null && nfe.Pagamentos.Any())
                {
                    dto.InfNFe.Pag = new PagDTO
                    {
                        DetPag = nfe.Pagamentos.Select(p => new DetPagDTO
                        {
                            IndPag = 0, // 0=Pagamento à vista
                            TPag = p.TipoPagamento ?? "01", // 01=Dinheiro
                            VPag = p.ValorPago
                        }).ToList(),
                        VTroco = 0
                    };
                }
                else
                {
                    // Se não houver pagamentos cadastrados, criar um pagamento padrão com o valor total
                    dto.InfNFe.Pag = new PagDTO
                    {
                        DetPag = new List<DetPagDTO>
                        {
                            new DetPagDTO
                            {
                                IndPag = 0, // 0=Pagamento à vista
                                TPag = "01", // 01=Dinheiro
                                VPag = nfe.ValorTotalNfe
                            }
                        },
                        VTroco = 0
                    };
                }

                // Informações adicionais
                dto.InfNFe.InfAdic = new InfAdicDTO
                {
                    InfCpl = $"NFe gerada pelo sistema. ID: {nfe.IdNfe}"
                };

                // Responsável técnico - buscar das configurações
                var respTecCNPJ = await ConfiguracaoHelper.BuscarValorConfiguracaoAsync(_context, "RESP_TEC_CNPJ");
                var respTecContato = await ConfiguracaoHelper.BuscarValorConfiguracaoAsync(_context, "RESP_TEC_CONTATO");
                var respTecEmail = await ConfiguracaoHelper.BuscarValorConfiguracaoAsync(_context, "RESP_TEC_EMAIL");
                var respTecFone = await ConfiguracaoHelper.BuscarValorConfiguracaoAsync(_context, "RESP_TEC_FONE");

                // Preencher informações do responsável técnico se pelo menos o CNPJ estiver configurado
                if (!string.IsNullOrWhiteSpace(respTecCNPJ))
                {
                    var cnpjLimpo = respTecCNPJ.Replace(".", "").Replace("/", "").Replace("-", "").Trim();
                    dto.InfNFe.InfRespTec = new InfRespTecDTO
                    {
                        CNPJ = cnpjLimpo,
                        XContato = respTecContato ?? "Responsável Técnico",
                        Email = respTecEmail ?? empresa.Email ?? "",
                        Fone = respTecFone ?? empresa.Fone ?? ""
                    };
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao converter NFe {idNfe} para DTO");
                return null;
            }
        }

        /// <summary>
        /// Envia uma NFe para a API NuvemFiscal
        /// </summary>
        public async Task<NFeResponseDTO?> EnviarNFeAsync(long idNfe)
        {
            try
            {
                var token = await ObterTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Não foi possível obter token da Nuvem Fiscal");
                    return null;
                }

                // Converter NFe para DTO
                var nfeDto = await ConverterNFeParaDTOAsync(idNfe);
                if (nfeDto == null)
                {
                    _logger.LogError($"Não foi possível converter NFe {idNfe} para DTO");
                    return null;
                }

                var httpClient = _httpClientFactory.CreateClient();
                var urlBase = await ObterUrlBaseApiAsync();
                httpClient.BaseAddress = new Uri($"{urlBase}/");
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // Serializar DTO para JSON (usar camelCase conforme API NuvemFiscal)
                var jsonOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonContent = JsonSerializer.Serialize(nfeDto, jsonOptions);
                
                // Log do JSON completo para debug
                _logger.LogInformation($"=== JSON ENVIADO PARA NUVEMFISCAL (NFe {idNfe}) ===");
                _logger.LogInformation(jsonContent);
                _logger.LogInformation($"=== FIM DO JSON ===");
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Enviando NFe {idNfe} para NuvemFiscal. URL: {urlBase}/nfe");

                var response = await httpClient.PostAsync("nfe", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                // Deserializar resposta independente do status HTTP
                NFeResponseDTO? responseDto = null;
                try
                {
                    responseDto = JsonSerializer.Deserialize<NFeResponseDTO>(responseContent, jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"Erro ao deserializar resposta da NuvemFiscal. Resposta: {responseContent}");
                }

                // Verificar se há formato de erro alternativo { "error": { "code": "...", "message": "..." } }
                if (responseDto != null && responseDto.Error != null)
                {
                    _logger.LogWarning($"NFe {idNfe} retornou formato de erro alternativo. Code: {responseDto.Error.Code}, Message: {responseDto.Error.Message}");
                    responseDto.Status = "erro";
                    if (responseDto.Erros == null)
                    {
                        responseDto.Erros = new List<ErroDTO>();
                    }
                    responseDto.Erros.Add(new ErroDTO
                    {
                        Codigo = responseDto.Error.Code,
                        Mensagem = responseDto.Error.Message ?? $"Erro: {responseDto.Error.Code}"
                    });
                }

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"NFe {idNfe} enviada. Status HTTP: {response.StatusCode}. Resposta: {responseContent}");

                    // Verificar se a resposta deserializada indica erro mesmo com HTTP 200
                    if (responseDto != null)
                    {
                        var statusUpper = responseDto.Status?.ToUpper() ?? "";
                        if (statusUpper == "ERRO" || statusUpper == "REJEITADO" || statusUpper.Contains("ERRO"))
                        {
                            _logger.LogWarning($"NFe {idNfe} retornou status de erro mesmo com HTTP 200. Status: {responseDto.Status}");
                            if (responseDto.Erros == null || !responseDto.Erros.Any())
                            {
                                responseDto.Erros = new List<ErroDTO>
                                {
                                    new ErroDTO { Mensagem = $"Status retornado: {responseDto.Status}" }
                                };
                            }
                        }
                    }

                    return responseDto ?? new NFeResponseDTO
                    {
                        Status = "erro",
                        Erros = new List<ErroDTO>
                        {
                            new ErroDTO { Mensagem = "Resposta da API não pôde ser processada." }
                        }
                    };
                }
                else
                {
                    _logger.LogError($"Erro ao enviar NFe {idNfe}. Status HTTP: {response.StatusCode}, Response: {responseContent}");

                    // Se conseguiu deserializar, retornar o DTO
                    if (responseDto != null)
                    {
                        // Garantir que o status seja de erro
                        if (string.IsNullOrEmpty(responseDto.Status) || 
                            (responseDto.Status.ToUpper() != "ERRO" && !responseDto.Status.ToUpper().Contains("ERRO")))
                        {
                            responseDto.Status = "erro";
                        }
                        
                        // Se não houver erros na lista, adicionar um erro genérico
                        if (responseDto.Erros == null || !responseDto.Erros.Any())
                        {
                            responseDto.Erros = new List<ErroDTO>
                            {
                                new ErroDTO
                                {
                                    Mensagem = $"Erro HTTP {response.StatusCode}: {responseContent}"
                                }
                            };
                        }
                        
                        return responseDto;
                    }

                    // Se não conseguiu deserializar, criar DTO de erro
                    return new NFeResponseDTO
                    {
                        Status = "erro",
                        Erros = new List<ErroDTO>
                        {
                            new ErroDTO
                            {
                                Mensagem = $"Erro HTTP {response.StatusCode}: {responseContent}"
                            }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar NFe {idNfe} para NuvemFiscal");
                return new NFeResponseDTO
                {
                    Status = "erro",
                    Erros = new List<ErroDTO>
                    {
                        new ErroDTO
                        {
                            Mensagem = ex.Message
                        }
                    }
                };
            }
        }
    }

    // Classes para deserialização JSON
    public class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public class CepResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("cep")]
        public string? Cep { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("logradouro")]
        public string? Logradouro { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("tipo_logradouro")]
        public string? TipoLogradouro { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("complemento")]
        public string? Complemento { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("bairro")]
        public string? Bairro { get; set; }
        
        // Mapeado manualmente no método BuscarCepAsync (municipio, localidade ou cidade)
        public string? Cidade { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("uf")]
        public string? Uf { get; set; }
        
        // Mapeado manualmente no método BuscarCepAsync (codigo_ibge ou ibge)
        public string? Ibge { get; set; }
    }
}

