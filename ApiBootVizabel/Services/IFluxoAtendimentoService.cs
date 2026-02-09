using ApiMensageria.Models;

namespace ApiMensageria.Services;

public interface IFluxoAtendimentoService
{
    Task<ProcessarMensagemResponse> ProcessarMensagemAsync(string idContato, string mensagem, string nomecontato);
}

