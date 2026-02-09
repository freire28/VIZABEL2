namespace ApiMensageria.Models;

public class Cliente
{
    // Campos para busca (resultado da query)
    public long Contador { get; set; }
    public int IdCliente { get; set; }
    public string NomeExibicao { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public string? NomeRazao { get; set; }
    public string? Fantasia { get; set; }
    
    // Campos completos da tabela CLIENTES
    public string NomeRazaoCompleto { get; set; } = string.Empty;
    public bool TipoPessoa { get; set; } // false = PF, true = PJ
    public string? FantasiaCompleto { get; set; }
    public string? CpfCnpjCompleto { get; set; }
    public string? EnderecoLogradouro { get; set; }
    public string? EnderecoNumero { get; set; }
    public string? EnderecoComplemento { get; set; }
    public string? EnderecoBairro { get; set; }
    public string? EnderecoCidade { get; set; }
    public string? EnderecoCep { get; set; }
    public string? Email { get; set; }
    public bool Ativo { get; set; } = true;
    public int? CodCliente { get; set; }
    public string? Contato { get; set; }
    public string? Fone { get; set; }
    public string? Whatsapp { get; set; }
    public string? FoneContato { get; set; }
    public string? RgIe { get; set; }
    public string? EnderecoUf { get; set; }
    public string? EnderecoIbge { get; set; }
}








