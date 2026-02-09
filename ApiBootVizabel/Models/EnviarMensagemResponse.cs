using System.Text.Json.Serialization;

namespace ApiMensageria.Models;

public class EnviarMensagemResponse
{
    [JsonPropertyName("idcontato")]
    public string? IdContato { get; set; }

    [JsonPropertyName("nomecontato")]
    public string? NomeContato { get; set; }

    [JsonPropertyName("mensagem")]
    public string? Mensagem { get; set; }
    
    [JsonPropertyName("erro")]
    public bool Erro { get; set; }

    [JsonPropertyName("pedidofinalizado")]
    public bool Pedidofinalizado { get; set; }

}

