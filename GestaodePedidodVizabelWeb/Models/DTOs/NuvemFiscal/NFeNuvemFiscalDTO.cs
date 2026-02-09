using System.Text.Json.Serialization;

namespace GestaoPedidosVizabel.Models.DTOs.NuvemFiscal
{
    /// <summary>
    /// DTO principal para envio de NFe para API NuvemFiscal
    /// Formato esperado pela API: { "infNFe": {...}, "infNFeSupl": {...}, "ambiente": "...", "referencia": "..." }
    /// </summary>
    public class NFeNuvemFiscalDTO
    {
        [JsonPropertyName("infNFe")]
        public InfNFeDTO InfNFe { get; set; } = new InfNFeDTO();

        [JsonPropertyName("infNFeSupl")]
        public InfNFeSuplDTO? InfNFeSupl { get; set; }

        [JsonPropertyName("ambiente")]
        public string Ambiente { get; set; } = "homologacao";

        [JsonPropertyName("referencia")]
        public string? Referencia { get; set; }
    }

    /// <summary>
    /// DTO para infNFe (informações da NFe)
    /// </summary>
    public class InfNFeDTO
    {
        [JsonPropertyName("versao")]
        public string Versao { get; set; } = "4.00";

        // Nota: O campo Id não é enviado no JSON, é usado apenas internamente para gerar o XML
        // A API NuvemFiscal gera o Id automaticamente baseado nos dados do ide

        [JsonPropertyName("ide")]
        public IdeDTO Ide { get; set; } = new IdeDTO();

        [JsonPropertyName("emit")]
        public EmitDTO? Emit { get; set; }

        [JsonPropertyName("dest")]
        public DestDTO? Dest { get; set; }

        [JsonPropertyName("det")]
        public List<DetDTO> Det { get; set; } = new List<DetDTO>();

        [JsonPropertyName("total")]
        public TotalDTO? Total { get; set; }

        [JsonPropertyName("transp")]
        public TranspDTO? Transp { get; set; }

        [JsonPropertyName("pag")]
        public PagDTO? Pag { get; set; }

        [JsonPropertyName("infAdic")]
        public InfAdicDTO? InfAdic { get; set; }

        [JsonPropertyName("infRespTec")]
        public InfRespTecDTO? InfRespTec { get; set; }
    }

    /// <summary>
    /// DTO para ide (identificação da NFe)
    /// </summary>
    public class IdeDTO
    {
        [JsonPropertyName("cUF")]
        public int CUF { get; set; }

        [JsonPropertyName("cNF")]
        public string? CNF { get; set; }

        [JsonPropertyName("natOp")]
        public string NatOp { get; set; } = string.Empty;

        [JsonPropertyName("mod")]
        public int Mod { get; set; } = 55; // 55 = NFe

        [JsonPropertyName("serie")]
        public int Serie { get; set; }

        [JsonPropertyName("nNF")]
        public int NNF { get; set; }

        [JsonPropertyName("dhEmi")]
        public string DhEmi { get; set; } = string.Empty;

        [JsonPropertyName("dhSaiEnt")]
        public string? DhSaiEnt { get; set; }

        [JsonPropertyName("tpNF")]
        public int TpNF { get; set; } // 0=Entrada, 1=Saída

        [JsonPropertyName("idDest")]
        public int IdDest { get; set; } // 1=Operação interna, 2=Operação interestadual, 3=Operação com exterior

        [JsonPropertyName("cMunFG")]
        public string CMunFG { get; set; } = string.Empty;

        [JsonPropertyName("tpImp")]
        public int TpImp { get; set; } = 1; // 1=Retrato

        [JsonPropertyName("tpEmis")]
        public int TpEmis { get; set; } = 1; // 1=Emissão normal

        [JsonPropertyName("cDV")]
        public int? CDV { get; set; }

        [JsonPropertyName("tpAmb")]
        public int TpAmb { get; set; } // 1=Produção, 2=Homologação

        [JsonPropertyName("finNFe")]
        public int FinNFe { get; set; } // 1=Normal, 2=Complementar, 3=Ajuste, 4=Devolução

        [JsonPropertyName("indFinal")]
        public int IndFinal { get; set; } // 0=Não, 1=Sim (consumidor final)

        [JsonPropertyName("indPres")]
        public int IndPres { get; set; } // 0=Não se aplica, 1=Operação presencial, etc

        [JsonPropertyName("procEmi")]
        public int ProcEmi { get; set; } = 0; // 0=Emissão de NF-e com aplicativo do contribuinte

        [JsonPropertyName("verProc")]
        public string VerProc { get; set; } = "GestaoPedidosVizabel";
    }

    /// <summary>
    /// DTO para emitente
    /// </summary>
    public class EmitDTO
    {
        [JsonPropertyName("CNPJ")]
        public string? CNPJ { get; set; }

        [JsonPropertyName("CPF")]
        public string? CPF { get; set; }

        [JsonPropertyName("xNome")]
        public string XNome { get; set; } = string.Empty;

        [JsonPropertyName("xFant")]
        public string? XFant { get; set; }

        [JsonPropertyName("enderEmit")]
        public EnderEmitDTO? EnderEmit { get; set; }

        [JsonPropertyName("IE")]
        public string? IE { get; set; }

        [JsonPropertyName("IEST")]
        public string? IEST { get; set; }

        [JsonPropertyName("IM")]
        public string? IM { get; set; }

        [JsonPropertyName("CNAE")]
        public string? CNAE { get; set; }

        [JsonPropertyName("CRT")]
        public int CRT { get; set; } // 1=Simples Nacional, 2=Simples Nacional - excesso de sublimite, 3=Regime Normal
    }

    /// <summary>
    /// DTO para endereço do emitente
    /// </summary>
    public class EnderEmitDTO
    {
        [JsonPropertyName("xLgr")]
        public string XLgr { get; set; } = string.Empty;

        [JsonPropertyName("nro")]
        public string Nro { get; set; } = string.Empty;

        [JsonPropertyName("xCpl")]
        public string? XCpl { get; set; }

        [JsonPropertyName("xBairro")]
        public string XBairro { get; set; } = string.Empty;

        [JsonPropertyName("cMun")]
        public string CMun { get; set; } = string.Empty;

        [JsonPropertyName("xMun")]
        public string XMun { get; set; } = string.Empty;

        [JsonPropertyName("UF")]
        public string UF { get; set; } = string.Empty;

        [JsonPropertyName("CEP")]
        public string CEP { get; set; } = string.Empty;

        [JsonPropertyName("cPais")]
        public string? CPais { get; set; } = "1058"; // Brasil

        [JsonPropertyName("xPais")]
        public string? XPais { get; set; } = "BRASIL";

        [JsonPropertyName("fone")]
        public string? Fone { get; set; }
    }

    /// <summary>
    /// DTO para destinatário
    /// </summary>
    public class DestDTO
    {
        [JsonPropertyName("CNPJ")]
        public string? CNPJ { get; set; }

        [JsonPropertyName("CPF")]
        public string? CPF { get; set; }

        [JsonPropertyName("idEstrangeiro")]
        public string? IdEstrangeiro { get; set; }

        [JsonPropertyName("xNome")]
        public string XNome { get; set; } = string.Empty;

        [JsonPropertyName("enderDest")]
        public EnderDestDTO? EnderDest { get; set; }

        [JsonPropertyName("indIEDest")]
        public int IndIEDest { get; set; } // 1=Contribuinte ICMS, 2=Contribuinte isento de IE, 9=Não contribuinte

        [JsonPropertyName("IE")]
        public string? IE { get; set; }

        [JsonPropertyName("ISUF")]
        public string? ISUF { get; set; }

        [JsonPropertyName("IM")]
        public string? IM { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    /// <summary>
    /// DTO para endereço do destinatário
    /// </summary>
    public class EnderDestDTO
    {
        [JsonPropertyName("xLgr")]
        public string XLgr { get; set; } = string.Empty;

        [JsonPropertyName("nro")]
        public string Nro { get; set; } = string.Empty;

        [JsonPropertyName("xCpl")]
        public string? XCpl { get; set; }

        [JsonPropertyName("xBairro")]
        public string XBairro { get; set; } = string.Empty;

        [JsonPropertyName("cMun")]
        public string CMun { get; set; } = string.Empty;

        [JsonPropertyName("xMun")]
        public string XMun { get; set; } = string.Empty;

        [JsonPropertyName("UF")]
        public string UF { get; set; } = string.Empty;

        [JsonPropertyName("CEP")]
        public string CEP { get; set; } = string.Empty;

        [JsonPropertyName("cPais")]
        public string? CPais { get; set; } = "1058"; // Brasil

        [JsonPropertyName("xPais")]
        public string? XPais { get; set; } = "BRASIL";

        [JsonPropertyName("fone")]
        public string? Fone { get; set; }
    }

    /// <summary>
    /// DTO para detalhes do item (det)
    /// </summary>
    public class DetDTO
    {
        [JsonPropertyName("nItem")]
        public int NItem { get; set; }

        [JsonPropertyName("prod")]
        public ProdDTO Prod { get; set; } = new ProdDTO();

        [JsonPropertyName("imposto")]
        public ImpostoDTO? Imposto { get; set; }
    }

    /// <summary>
    /// DTO para produto
    /// </summary>
    public class ProdDTO
    {
        [JsonPropertyName("cProd")]
        public string CProd { get; set; } = string.Empty;

        [JsonPropertyName("cEAN")]
        public string? CEAN { get; set; }

        [JsonPropertyName("xProd")]
        public string XProd { get; set; } = string.Empty;

        [JsonPropertyName("NCM")]
        public string NCM { get; set; } = string.Empty;

        [JsonPropertyName("CEST")]
        public string? CEST { get; set; }

        [JsonPropertyName("CFOP")]
        public string CFOP { get; set; } = string.Empty;

        [JsonPropertyName("uCom")]
        public string UCom { get; set; } = "UN";

        [JsonPropertyName("qCom")]
        public decimal QCom { get; set; }

        [JsonPropertyName("vUnCom")]
        public decimal VUnCom { get; set; }

        [JsonPropertyName("vProd")]
        public decimal VProd { get; set; }

        [JsonPropertyName("cEANTrib")]
        public string? CEANTrib { get; set; }

        [JsonPropertyName("uTrib")]
        public string UTrib { get; set; } = "UN";

        [JsonPropertyName("qTrib")]
        public decimal QTrib { get; set; }

        [JsonPropertyName("vUnTrib")]
        public decimal VUnTrib { get; set; }

        [JsonPropertyName("indTot")]
        public int IndTot { get; set; } = 1; // 1=Valor do item compõe o valor total da NF-e
    }

    /// <summary>
    /// DTO para impostos
    /// </summary>
    public class ImpostoDTO
    {
        [JsonPropertyName("vTotTrib")]
        public decimal? VTotTrib { get; set; }

        [JsonPropertyName("ICMS")]
        public ICMSDTO? ICMS { get; set; }

        [JsonPropertyName("IPI")]
        public IPIDTO? IPI { get; set; }

        [JsonPropertyName("PIS")]
        public PISDTO? PIS { get; set; }

        [JsonPropertyName("COFINS")]
        public COFINSDTO? COFINS { get; set; }
    }

    /// <summary>
    /// DTO para ICMS (Simples Nacional usa CSOSN)
    /// </summary>
    public class ICMSDTO
    {
        // Para Simples Nacional - CSOSN 101, 102, 201, 202, 500, 900
        [JsonPropertyName("ICMSSN101")]
        public ICMSSN101DTO? ICMSSN101 { get; set; }

        [JsonPropertyName("ICMSSN102")]
        public ICMSSN102DTO? ICMSSN102 { get; set; }

        [JsonPropertyName("ICMSSN201")]
        public ICMSSN201DTO? ICMSSN201 { get; set; }

        [JsonPropertyName("ICMSSN202")]
        public ICMSSN202DTO? ICMSSN202 { get; set; }

        [JsonPropertyName("ICMSSN500")]
        public ICMSSN500DTO? ICMSSN500 { get; set; }

        [JsonPropertyName("ICMSSN900")]
        public ICMSSN900DTO? ICMSSN900 { get; set; }

        // Para Regime Normal - CST
        [JsonPropertyName("ICMS00")]
        public ICMS00DTO? ICMS00 { get; set; }

        [JsonPropertyName("ICMS20")]
        public ICMS20DTO? ICMS20 { get; set; }

        [JsonPropertyName("ICMS40")]
        public ICMS40DTO? ICMS40 { get; set; }

        [JsonPropertyName("ICMS90")]
        public ICMS90DTO? ICMS90 { get; set; }
    }

    /// <summary>
    /// DTO para ICMS Simples Nacional 101 (Tributada pelo Simples Nacional com permissão de crédito)
    /// </summary>
    public class ICMSSN101DTO
    {
        [JsonPropertyName("orig")]
        public int Orig { get; set; }

        [JsonPropertyName("CSOSN")]
        public string CSOSN { get; set; } = "101";

        [JsonPropertyName("pCredSN")]
        public decimal? PCredSN { get; set; }

        [JsonPropertyName("vCredICMSSN")]
        public decimal? VCredICMSSN { get; set; }
    }

    /// <summary>
    /// DTO para ICMS Simples Nacional 102 (Tributada pelo Simples Nacional sem permissão de crédito)
    /// </summary>
    public class ICMSSN102DTO
    {
        [JsonPropertyName("orig")]
        public int Orig { get; set; }

        [JsonPropertyName("CSOSN")]
        public string CSOSN { get; set; } = "102";
    }

    /// <summary>
    /// DTO para ICMS Simples Nacional 201 (Tributada pelo Simples Nacional com permissão de crédito e com cobrança do ICMS por Substituição Tributária)
    /// </summary>
    public class ICMSSN201DTO
    {
        [JsonPropertyName("orig")]
        public int Orig { get; set; }

        [JsonPropertyName("CSOSN")]
        public string CSOSN { get; set; } = "201";

        [JsonPropertyName("modBCST")]
        public int? ModBCST { get; set; }

        [JsonPropertyName("pMVAST")]
        public decimal? PMVAST { get; set; }

        [JsonPropertyName("pRedBCST")]
        public decimal? PRedBCST { get; set; }

        [JsonPropertyName("vBCST")]
        public decimal? VBCST { get; set; }

        [JsonPropertyName("pICMSST")]
        public decimal? PICMSST { get; set; }

        [JsonPropertyName("vICMSST")]
        public decimal? VICMSST { get; set; }

        [JsonPropertyName("pCredSN")]
        public decimal? PCredSN { get; set; }

        [JsonPropertyName("vCredICMSSN")]
        public decimal? VCredICMSSN { get; set; }
    }

    /// <summary>
    /// DTO para ICMS Simples Nacional 202 (Tributada pelo Simples Nacional sem permissão de crédito e com cobrança do ICMS por Substituição Tributária)
    /// </summary>
    public class ICMSSN202DTO
    {
        [JsonPropertyName("orig")]
        public int Orig { get; set; }

        [JsonPropertyName("CSOSN")]
        public string CSOSN { get; set; } = "202";

        [JsonPropertyName("modBCST")]
        public int? ModBCST { get; set; }

        [JsonPropertyName("pMVAST")]
        public decimal? PMVAST { get; set; }

        [JsonPropertyName("pRedBCST")]
        public decimal? PRedBCST { get; set; }

        [JsonPropertyName("vBCST")]
        public decimal? VBCST { get; set; }

        [JsonPropertyName("pICMSST")]
        public decimal? PICMSST { get; set; }

        [JsonPropertyName("vICMSST")]
        public decimal? VICMSST { get; set; }
    }

    /// <summary>
    /// DTO para ICMS Simples Nacional 500 (ICMS cobrado anteriormente por substituição tributária)
    /// </summary>
    public class ICMSSN500DTO
    {
        [JsonPropertyName("orig")]
        public int Orig { get; set; }

        [JsonPropertyName("CSOSN")]
        public string CSOSN { get; set; } = "500";
    }

    /// <summary>
    /// DTO para ICMS Simples Nacional 900 (Outros)
    /// </summary>
    public class ICMSSN900DTO
    {
        [JsonPropertyName("orig")]
        public int Orig { get; set; }

        [JsonPropertyName("CSOSN")]
        public string CSOSN { get; set; } = "900";

        [JsonPropertyName("modBC")]
        public int? ModBC { get; set; }

        [JsonPropertyName("vBC")]
        public decimal? VBC { get; set; }

        [JsonPropertyName("pRedBC")]
        public decimal? PRedBC { get; set; }

        [JsonPropertyName("pICMS")]
        public decimal? PICMS { get; set; }

        [JsonPropertyName("vICMS")]
        public decimal? VICMS { get; set; }

        [JsonPropertyName("modBCST")]
        public int? ModBCST { get; set; }

        [JsonPropertyName("pMVAST")]
        public decimal? PMVAST { get; set; }

        [JsonPropertyName("pRedBCST")]
        public decimal? PRedBCST { get; set; }

        [JsonPropertyName("vBCST")]
        public decimal? VBCST { get; set; }

        [JsonPropertyName("pICMSST")]
        public decimal? PICMSST { get; set; }

        [JsonPropertyName("vICMSST")]
        public decimal? VICMSST { get; set; }

        [JsonPropertyName("pCredSN")]
        public decimal? PCredSN { get; set; }

        [JsonPropertyName("vCredICMSSN")]
        public decimal? VCredICMSSN { get; set; }
    }

    /// <summary>
    /// DTO para ICMS 00 (Tributada integralmente)
    /// </summary>
    public class ICMS00DTO
    {
        [JsonPropertyName("orig")]
        public int Orig { get; set; }

        [JsonPropertyName("CST")]
        public string CST { get; set; } = "00";

        [JsonPropertyName("modBC")]
        public int ModBC { get; set; }

        [JsonPropertyName("vBC")]
        public decimal VBC { get; set; }

        [JsonPropertyName("pICMS")]
        public decimal PICMS { get; set; }

        [JsonPropertyName("vICMS")]
        public decimal VICMS { get; set; }
    }

    /// <summary>
    /// DTO para ICMS 20 (Com redução de base de cálculo)
    /// </summary>
    public class ICMS20DTO
    {
        [JsonPropertyName("orig")]
        public int Orig { get; set; }

        [JsonPropertyName("CST")]
        public string CST { get; set; } = "20";

        [JsonPropertyName("modBC")]
        public int ModBC { get; set; }

        [JsonPropertyName("pRedBC")]
        public decimal? PRedBC { get; set; }

        [JsonPropertyName("vBC")]
        public decimal VBC { get; set; }

        [JsonPropertyName("pICMS")]
        public decimal PICMS { get; set; }

        [JsonPropertyName("vICMS")]
        public decimal VICMS { get; set; }
    }

    /// <summary>
    /// DTO para ICMS 40 (Isenta / não tributada)
    /// </summary>
    public class ICMS40DTO
    {
        [JsonPropertyName("orig")]
        public int Orig { get; set; }

        [JsonPropertyName("CST")]
        public string CST { get; set; } = "40";
    }

    /// <summary>
    /// DTO para ICMS 90 (Outras)
    /// </summary>
    public class ICMS90DTO
    {
        [JsonPropertyName("orig")]
        public int Orig { get; set; }

        [JsonPropertyName("CST")]
        public string CST { get; set; } = "90";

        [JsonPropertyName("modBC")]
        public int? ModBC { get; set; }

        [JsonPropertyName("vBC")]
        public decimal? VBC { get; set; }

        [JsonPropertyName("pRedBC")]
        public decimal? PRedBC { get; set; }

        [JsonPropertyName("pICMS")]
        public decimal? PICMS { get; set; }

        [JsonPropertyName("vICMS")]
        public decimal? VICMS { get; set; }
    }

    /// <summary>
    /// DTO para IPI
    /// </summary>
    public class IPIDTO
    {
        [JsonPropertyName("cEnq")]
        public string? CEnq { get; set; }

        [JsonPropertyName("IPITrib")]
        public IPITribDTO? IPITrib { get; set; }

        [JsonPropertyName("IPINT")]
        public IPINTDTO? IPINT { get; set; }
    }

    /// <summary>
    /// DTO para IPI tributado
    /// </summary>
    public class IPITribDTO
    {
        [JsonPropertyName("CST")]
        public string CST { get; set; } = string.Empty;

        [JsonPropertyName("vBC")]
        public decimal? VBC { get; set; }

        [JsonPropertyName("pIPI")]
        public decimal? PIPI { get; set; }

        [JsonPropertyName("vIPI")]
        public decimal? VIPI { get; set; }
    }

    /// <summary>
    /// DTO para IPI não tributado
    /// </summary>
    public class IPINTDTO
    {
        [JsonPropertyName("CST")]
        public string CST { get; set; } = "01"; // 01=Entrada tributada com alíquota zero
    }

    /// <summary>
    /// DTO para PIS
    /// </summary>
    public class PISDTO
    {
        [JsonPropertyName("PISAliq")]
        public PISAliqDTO? PISAliq { get; set; }

        [JsonPropertyName("PISQtde")]
        public PISQtdeDTO? PISQtde { get; set; }

        [JsonPropertyName("PISNT")]
        public PISNTDTO? PISNT { get; set; }

        [JsonPropertyName("PISOutr")]
        public PISOutrDTO? PISOutr { get; set; }
    }

    /// <summary>
    /// DTO para PIS Alíquota
    /// </summary>
    public class PISAliqDTO
    {
        [JsonPropertyName("CST")]
        public string CST { get; set; } = "01"; // 01=Operação Tributável (base de cálculo = valor da operação alíquota normal)

        [JsonPropertyName("vBC")]
        public decimal VBC { get; set; }

        [JsonPropertyName("pPIS")]
        public decimal PPIS { get; set; }

        [JsonPropertyName("vPIS")]
        public decimal VPIS { get; set; }
    }

    /// <summary>
    /// DTO para PIS Quantidade
    /// </summary>
    public class PISQtdeDTO
    {
        [JsonPropertyName("CST")]
        public string CST { get; set; } = "03"; // 03=Operação Tributável (base de cálculo = quantidade vendida x alíquota por unidade de produto)

        [JsonPropertyName("qBCProd")]
        public decimal QBCProd { get; set; }

        [JsonPropertyName("vAliqProd")]
        public decimal VAliqProd { get; set; }

        [JsonPropertyName("vPIS")]
        public decimal VPIS { get; set; }
    }

    /// <summary>
    /// DTO para PIS Não Tributado
    /// </summary>
    public class PISNTDTO
    {
        [JsonPropertyName("CST")]
        public string CST { get; set; } = "04"; // 04=Operação Tributável (tributação monofásica (alíquota zero))
    }

    /// <summary>
    /// DTO para PIS Outras Operações
    /// </summary>
    public class PISOutrDTO
    {
        [JsonPropertyName("CST")]
        public string CST { get; set; } = string.Empty;

        [JsonPropertyName("vBC")]
        public decimal? VBC { get; set; }

        [JsonPropertyName("pPIS")]
        public decimal? PPIS { get; set; }

        [JsonPropertyName("qBCProd")]
        public decimal? QBCProd { get; set; }

        [JsonPropertyName("vAliqProd")]
        public decimal? VAliqProd { get; set; }

        [JsonPropertyName("vPIS")]
        public decimal VPIS { get; set; }
    }

    /// <summary>
    /// DTO para COFINS
    /// </summary>
    public class COFINSDTO
    {
        [JsonPropertyName("COFINSAliq")]
        public COFINSAliqDTO? COFINSAliq { get; set; }

        [JsonPropertyName("COFINSQtde")]
        public COFINSQtdeDTO? COFINSQtde { get; set; }

        [JsonPropertyName("COFINSNT")]
        public COFINSNTDTO? COFINSNT { get; set; }

        [JsonPropertyName("COFINSOutr")]
        public COFINSOutrDTO? COFINSOutr { get; set; }
    }

    /// <summary>
    /// DTO para COFINS Alíquota
    /// </summary>
    public class COFINSAliqDTO
    {
        [JsonPropertyName("CST")]
        public string CST { get; set; } = "01"; // 01=Operação Tributável (base de cálculo = valor da operação alíquota normal)

        [JsonPropertyName("vBC")]
        public decimal VBC { get; set; }

        [JsonPropertyName("pCOFINS")]
        public decimal PCOFINS { get; set; }

        [JsonPropertyName("vCOFINS")]
        public decimal VCOFINS { get; set; }
    }

    /// <summary>
    /// DTO para COFINS Quantidade
    /// </summary>
    public class COFINSQtdeDTO
    {
        [JsonPropertyName("CST")]
        public string CST { get; set; } = "03"; // 03=Operação Tributável (base de cálculo = quantidade vendida x alíquota por unidade de produto)

        [JsonPropertyName("qBCProd")]
        public decimal QBCProd { get; set; }

        [JsonPropertyName("vAliqProd")]
        public decimal VAliqProd { get; set; }

        [JsonPropertyName("vCOFINS")]
        public decimal VCOFINS { get; set; }
    }

    /// <summary>
    /// DTO para COFINS Não Tributado
    /// </summary>
    public class COFINSNTDTO
    {
        [JsonPropertyName("CST")]
        public string CST { get; set; } = "04"; // 04=Operação Tributável (tributação monofásica (alíquota zero))
    }

    /// <summary>
    /// DTO para COFINS Outras Operações
    /// </summary>
    public class COFINSOutrDTO
    {
        [JsonPropertyName("CST")]
        public string CST { get; set; } = string.Empty;

        [JsonPropertyName("vBC")]
        public decimal? VBC { get; set; }

        [JsonPropertyName("pCOFINS")]
        public decimal? PCOFINS { get; set; }

        [JsonPropertyName("qBCProd")]
        public decimal? QBCProd { get; set; }

        [JsonPropertyName("vAliqProd")]
        public decimal? VAliqProd { get; set; }

        [JsonPropertyName("vCOFINS")]
        public decimal VCOFINS { get; set; }
    }

    /// <summary>
    /// DTO para totais da NFe
    /// </summary>
    public class TotalDTO
    {
        [JsonPropertyName("ICMSTot")]
        public ICMSTotDTO? ICMSTot { get; set; }
    }

    /// <summary>
    /// DTO para totais de ICMS
    /// </summary>
    public class ICMSTotDTO
    {
        [JsonPropertyName("vBC")]
        public decimal? VBC { get; set; }

        [JsonPropertyName("vICMS")]
        public decimal? VICMS { get; set; }

        [JsonPropertyName("vICMSDeson")]
        public decimal? VICMSDeson { get; set; }

        [JsonPropertyName("vFCP")]
        public decimal? VFCP { get; set; }

        [JsonPropertyName("vBCST")]
        public decimal? VBCST { get; set; }

        [JsonPropertyName("vST")]
        public decimal? VST { get; set; }

        [JsonPropertyName("vFCPST")]
        public decimal? VFCPST { get; set; }

        [JsonPropertyName("vFCPSTRet")]
        public decimal? VFCPSTRet { get; set; }

        [JsonPropertyName("vProd")]
        public decimal VProd { get; set; }

        [JsonPropertyName("vFrete")]
        public decimal? VFrete { get; set; }

        [JsonPropertyName("vSeg")]
        public decimal? VSeg { get; set; }

        [JsonPropertyName("vDesc")]
        public decimal? VDesc { get; set; }

        [JsonPropertyName("vII")]
        public decimal? VII { get; set; }

        [JsonPropertyName("vIPI")]
        public decimal? VIPI { get; set; }

        [JsonPropertyName("vIPIDevol")]
        public decimal? VIPIDevol { get; set; }

        [JsonPropertyName("vPIS")]
        public decimal? VPIS { get; set; }

        [JsonPropertyName("vCOFINS")]
        public decimal? VCOFINS { get; set; }

        [JsonPropertyName("vOutro")]
        public decimal? VOutro { get; set; }

        [JsonPropertyName("vNF")]
        public decimal VNF { get; set; }

        [JsonPropertyName("vTotTrib")]
        public decimal? VTotTrib { get; set; }
    }

    /// <summary>
    /// DTO para transporte
    /// </summary>
    public class TranspDTO
    {
        [JsonPropertyName("modFrete")]
        public int ModFrete { get; set; } // 0=Por conta do emitente, 1=Por conta do destinatário, etc
    }

    /// <summary>
    /// DTO para pagamento
    /// </summary>
    public class PagDTO
    {
        [JsonPropertyName("detPag")]
        public List<DetPagDTO> DetPag { get; set; } = new List<DetPagDTO>();

        [JsonPropertyName("vTroco")]
        public decimal? VTroco { get; set; }
    }

    /// <summary>
    /// DTO para detalhes de pagamento
    /// </summary>
    public class DetPagDTO
    {
        [JsonPropertyName("indPag")]
        public int? IndPag { get; set; } // 0=Pagamento à vista, 1=Pagamento a prazo

        [JsonPropertyName("tPag")]
        public string TPag { get; set; } = string.Empty; // 01=Dinheiro, 02=Cheque, etc

        [JsonPropertyName("xPag")]
        public string? XPag { get; set; }

        [JsonPropertyName("vPag")]
        public decimal VPag { get; set; }
    }

    /// <summary>
    /// DTO para informações adicionais
    /// </summary>
    public class InfAdicDTO
    {
        [JsonPropertyName("infAdFisco")]
        public string? InfAdFisco { get; set; }

        [JsonPropertyName("infCpl")]
        public string? InfCpl { get; set; }
    }

    /// <summary>
    /// DTO para informações do responsável técnico
    /// </summary>
    public class InfRespTecDTO
    {
        [JsonPropertyName("CNPJ")]
        public string CNPJ { get; set; } = string.Empty;

        [JsonPropertyName("xContato")]
        public string XContato { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("fone")]
        public string Fone { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para informações suplementares da NFe
    /// </summary>
    public class InfNFeSuplDTO
    {
        [JsonPropertyName("qrCode")]
        public string? QrCode { get; set; }

        [JsonPropertyName("urlChave")]
        public string? UrlChave { get; set; }
    }

    /// <summary>
    /// DTO para resposta da API ao criar NFe
    /// </summary>
    public class NFeResponseDTO
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("chave_acesso")]
        public string? ChaveAcesso { get; set; }

        [JsonPropertyName("chave")]
        public string? Chave { get; set; }

        [JsonPropertyName("xml")]
        public string? Xml { get; set; }

        [JsonPropertyName("danfe")]
        public string? Danfe { get; set; }

        [JsonPropertyName("erros")]
        public List<ErroDTO>? Erros { get; set; }

        // Formato alternativo de erro da API
        [JsonPropertyName("error")]
        public ErrorObjectDTO? Error { get; set; }

        [JsonPropertyName("autorizacao")]
        public AutorizacaoDTO? Autorizacao { get; set; }
    }

    /// <summary>
    /// DTO para informações de autorização da NFe
    /// </summary>
    public class AutorizacaoDTO
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("chave_acesso")]
        public string? ChaveAcesso { get; set; }

        [JsonPropertyName("codigo_status")]
        public int? CodigoStatus { get; set; }

        [JsonPropertyName("motivo_status")]
        public string? MotivoStatus { get; set; }

        [JsonPropertyName("tipo_evento")]
        public string? TipoEvento { get; set; }
    }

    /// <summary>
    /// DTO para erros da API
    /// </summary>
    public class ErroDTO
    {
        [JsonPropertyName("codigo")]
        public string? Codigo { get; set; }

        [JsonPropertyName("mensagem")]
        public string? Mensagem { get; set; }

        [JsonPropertyName("correcao")]
        public string? Correcao { get; set; }
    }

    /// <summary>
    /// DTO para formato alternativo de erro da API (ex: { "error": { "code": "...", "message": "..." } })
    /// </summary>
    public class ErrorObjectDTO
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
