using Microsoft.AspNetCore.Mvc;
using ApiMensageria.Models;
using ApiMensageria.Services;

namespace ApiMensageria.Controllers;

[ApiController]
public class MensagemController : ControllerBase
{
    private readonly IFluxoAtendimentoService _fluxoAtendimentoService;

    public MensagemController(IFluxoAtendimentoService fluxoAtendimentoService)
    {
        _fluxoAtendimentoService = fluxoAtendimentoService;
    }

    [HttpPost("/enviarmensagem")]
    public async Task<IActionResult> EnviarMensagem([FromBody] EnviarMensagemRequest request)
    {
        try
        {
            // Validação dos campos obrigatórios
            if (string.IsNullOrWhiteSpace(request.IdContato) || string.IsNullOrWhiteSpace(request.Mensagem))
            {
                return BadRequest(new EnviarMensagemResponse
                {
                    IdContato = request.IdContato,
                    Mensagem = request.Mensagem,
                    NomeContato = request.NomeContato,
                    Erro = true
                });
            }

            // Processa a mensagem através do fluxo de atendimento
            var resposta = await _fluxoAtendimentoService.ProcessarMensagemAsync(request.IdContato, request.Mensagem, request.NomeContato);

            // Resposta de sucesso com a mensagem processada
            return Ok(new EnviarMensagemResponse
            {
                IdContato = request.IdContato,
                Mensagem = resposta.Mensagem,
                NomeContato = request.NomeContato,
                Pedidofinalizado = resposta.PedidoFinalizado,
                Erro = false
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new EnviarMensagemResponse
            {
                IdContato = request?.IdContato,
                Mensagem = request?.Mensagem,
                NomeContato = request.NomeContato,
                Erro = true
            });
        }
    }
}

