namespace ApiMensageria.Models;

public class Configuracao
{
    public int IdConfiguracao { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public bool? ConsiderarNoPrazoEntrega { get; set; }
}




