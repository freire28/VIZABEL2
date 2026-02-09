using System.Text.Json.Serialization;

namespace ApiMensageria.Models;

public class EnviarMensagemRequest
{
    [JsonPropertyName("idcontato")]
    public string? IdContato { get; set; }

    [JsonPropertyName("nomecontato")]
    public string? NomeContato { get; set; }

    [JsonPropertyName("mensagem")]
    public string? Mensagem { get; set; }
}

