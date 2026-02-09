using ApiMensageria.Models;
using ApiMensageria.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ApiMensageria.Tests.Services;

public class FluxoAtendimentoServiceTests
{
    private readonly FluxoAtendimentoService _service;
    private readonly Mock<IClienteService> _mockClienteService;
    private readonly Mock<IProdutoService> _mockProdutoService;
    private readonly Mock<IFormaPagamentoService> _mockFormaPagamentoService;
    private readonly Mock<IPedidoService> _mockPedidoService;
    private readonly Mock<IConfiguracaoService> _mockConfiguracaoService;
    private readonly IServiceProvider _serviceProvider;

    public FluxoAtendimentoServiceTests()
    {
        _mockClienteService = new Mock<IClienteService>();
        _mockProdutoService = new Mock<IProdutoService>();
        _mockFormaPagamentoService = new Mock<IFormaPagamentoService>();
        _mockPedidoService = new Mock<IPedidoService>();
        _mockConfiguracaoService = new Mock<IConfiguracaoService>();

        var services = new ServiceCollection();
        services.AddSingleton(_mockClienteService.Object);
        services.AddSingleton(_mockProdutoService.Object);
        services.AddSingleton(_mockFormaPagamentoService.Object);
        services.AddSingleton(_mockPedidoService.Object);
        services.AddSingleton(_mockConfiguracaoService.Object);
        _serviceProvider = services.BuildServiceProvider();

        _service = new FluxoAtendimentoService(_serviceProvider);
    }

    [Fact]
    public async Task ProcessarMensagemAsync_EstadoInicial_RetornaMenuInicialComNomeDoContato()
    {
        // Arrange
        var idContato = "contato-novo-123";
        var mensagem = "qualquer";
        var nomeContato = "Carlos Silva";

        // Act
        var result = await _service.ProcessarMensagemAsync(idContato, mensagem, nomeContato);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.PedidoFinalizado);
        Assert.Contains("Carlos Silva", result.Mensagem);
        Assert.Contains("Fazer um Pedido", result.Mensagem);
        Assert.Contains("Verificar Andamento", result.Mensagem);
        Assert.Contains("0️⃣ Encerrar", result.Mensagem);
    }

    [Fact]
    public async Task ProcessarMensagemAsync_MensagemZero_EncerraAtendimento()
    {
        // Arrange: primeira mensagem para criar contato e ir para menu
        await _service.ProcessarMensagemAsync("contato-1", "oi", "Ana");
        // Envia "0" para encerrar
        // Act
        var result = await _service.ProcessarMensagemAsync("contato-1", "0", "Ana");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Atendimento encerrado", result.Mensagem);
        Assert.Contains("Ana", result.Mensagem);
    }

    [Fact]
    public async Task ProcessarMensagemAsync_Opcao1NoMenu_RetornaMenuSelecaoCliente()
    {
        // Arrange: vai para menu (estado AguardandoOpcao)
        await _service.ProcessarMensagemAsync("contato-2", "ola", "Bruno");
        // Act: opção 1 = Fazer Pedido -> seleção de cliente
        var result = await _service.ProcessarMensagemAsync("contato-2", "1", "Bruno");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Selecionar o Cliente", result.Mensagem);
        Assert.Contains("Nome, CPF ou CNPJ", result.Mensagem);
        Assert.Contains("Novo Cliente", result.Mensagem);
    }

    [Fact]
    public async Task ProcessarMensagemAsync_OpcaoInvalidaNoMenu_RetornaMensagemDeErro()
    {
        // Arrange
        await _service.ProcessarMensagemAsync("contato-3", "ola", "Clara");
        // Act: opção inválida
        var result = await _service.ProcessarMensagemAsync("contato-3", "99", "Clara");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Opção inválida", result.Mensagem);
    }

    [Fact]
    public async Task ProcessarMensagemAsync_Opcao2NoMenu_RetornaMenuVerificarPedido()
    {
        // Arrange
        await _service.ProcessarMensagemAsync("contato-4", "ola", "Diego");
        // Act: opção 2 = Verificar Pedido
        var result = await _service.ProcessarMensagemAsync("contato-4", "2", "Diego");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Verificar Andamento de Pedido", result.Mensagem);
        Assert.Contains("Buscar pedido pelo Código", result.Mensagem);
        Assert.Contains("Buscar pelo nome do Cliente", result.Mensagem);
    }

    [Fact]
    public async Task ProcessarMensagemAsync_DoisContatosDiferentes_MantemEstadosSeparados()
    {
        // Arrange & Act: contato A vai para menu e escolhe 1
        await _service.ProcessarMensagemAsync("contato-A", "oi", "Alice");
        var resultA = await _service.ProcessarMensagemAsync("contato-A", "1", "Alice");

        // Contato B vai para menu e escolhe 2
        await _service.ProcessarMensagemAsync("contato-B", "oi", "Bob");
        var resultB = await _service.ProcessarMensagemAsync("contato-B", "2", "Bob");

        // Assert
        Assert.Contains("Selecionar o Cliente", resultA.Mensagem);
        Assert.Contains("Verificar Andamento", resultB.Mensagem);
    }
}
