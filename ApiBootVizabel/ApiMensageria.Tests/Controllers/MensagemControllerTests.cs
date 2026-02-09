using ApiMensageria.Controllers;
using ApiMensageria.Models;
using ApiMensageria.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ApiMensageria.Tests.Controllers;

public class MensagemControllerTests
{
    private readonly Mock<IFluxoAtendimentoService> _mockFluxoAtendimentoService;
    private readonly MensagemController _controller;

    public MensagemControllerTests()
    {
        _mockFluxoAtendimentoService = new Mock<IFluxoAtendimentoService>();
        _controller = new MensagemController(_mockFluxoAtendimentoService.Object);
    }

    [Fact]
    public async Task EnviarMensagem_ComCamposValidos_RetornaOkComRespostaProcessada()
    {
        // Arrange
        var request = new EnviarMensagemRequest
        {
            IdContato = "contato123",
            Mensagem = "Olá",
            NomeContato = "João"
        };
        var respostaEsperada = new ProcessarMensagemResponse
        {
            Mensagem = "Menu inicial...",
            PedidoFinalizado = false
        };
        _mockFluxoAtendimentoService
            .Setup(s => s.ProcessarMensagemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(respostaEsperada);

        // Act
        var result = await _controller.EnviarMensagem(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<EnviarMensagemResponse>(okResult.Value);
        Assert.False(response.Erro);
        Assert.Equal(request.IdContato, response.IdContato);
        Assert.Equal(respostaEsperada.Mensagem, response.Mensagem);
        Assert.Equal(respostaEsperada.PedidoFinalizado, response.Pedidofinalizado);
        _mockFluxoAtendimentoService.Verify(
            s => s.ProcessarMensagemAsync("contato123", "Olá", "João"),
            Times.Once);
    }

    [Fact]
    public async Task EnviarMensagem_ComIdContatoVazio_RetornaBadRequest()
    {
        // Arrange
        var request = new EnviarMensagemRequest
        {
            IdContato = "",
            Mensagem = "Olá",
            NomeContato = "João"
        };

        // Act
        var result = await _controller.EnviarMensagem(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<EnviarMensagemResponse>(badRequestResult.Value);
        Assert.True(response.Erro);
        _mockFluxoAtendimentoService.Verify(
            s => s.ProcessarMensagemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task EnviarMensagem_ComMensagemVazia_RetornaBadRequest()
    {
        // Arrange
        var request = new EnviarMensagemRequest
        {
            IdContato = "contato123",
            Mensagem = "",
            NomeContato = "João"
        };

        // Act
        var result = await _controller.EnviarMensagem(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<EnviarMensagemResponse>(badRequestResult.Value);
        Assert.True(response.Erro);
    }

    [Fact]
    public async Task EnviarMensagem_ComIdContatoNull_RetornaBadRequest()
    {
        // Arrange
        var request = new EnviarMensagemRequest
        {
            IdContato = null,
            Mensagem = "Olá",
            NomeContato = "João"
        };

        // Act
        var result = await _controller.EnviarMensagem(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.True(((EnviarMensagemResponse)badRequestResult.Value!).Erro);
    }

    [Fact]
    public async Task EnviarMensagem_QuandoServicoLancaExcecao_RetornaStatusCode500()
    {
        // Arrange
        var request = new EnviarMensagemRequest
        {
            IdContato = "contato123",
            Mensagem = "Olá",
            NomeContato = "João"
        };
        _mockFluxoAtendimentoService
            .Setup(s => s.ProcessarMensagemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Erro interno"));

        // Act
        var result = await _controller.EnviarMensagem(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
        var response = Assert.IsType<EnviarMensagemResponse>(statusResult.Value);
        Assert.True(response.Erro);
    }

    [Fact]
    public async Task EnviarMensagem_ComPedidoFinalizado_RetornaOkComPedidofinalizadoTrue()
    {
        // Arrange
        var request = new EnviarMensagemRequest
        {
            IdContato = "contato123",
            Mensagem = "1",
            NomeContato = "Maria"
        };
        var respostaEsperada = new ProcessarMensagemResponse
        {
            Mensagem = "Pedido finalizado com sucesso!",
            PedidoFinalizado = true
        };
        _mockFluxoAtendimentoService
            .Setup(s => s.ProcessarMensagemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(respostaEsperada);

        // Act
        var result = await _controller.EnviarMensagem(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<EnviarMensagemResponse>(okResult.Value);
        Assert.True(response.Pedidofinalizado);
    }
}
