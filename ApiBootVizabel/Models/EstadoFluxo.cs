namespace ApiMensageria.Models;

public enum EstadoFluxo
{
    Inicial,
    AguardandoOpcao,
    SelecionandoCliente,
    ConfirmandoCliente,
    CadastrandoClienteNome,
    CadastrandoClienteCpfCnpj,
    CadastrandoClienteTelefone,
    CadastrandoClienteEndereco,
    CadastrandoClienteTemplate,
    AguardandoTemplatePreenchido,
    BuscandoProduto,
    SelecionandoTamanho,
    PerguntandoInserirImagem,
    AguardandoImagem,
    PerguntandoMaisProduto,
    FinalizandoPedido,
    SelecionandoFormaPagamento,
    PerguntandoEmitirNfe,
    FazendoPedido,
    VerificandoPedido,
    VerificandoPedidoTipoBusca,
    VerificandoPedidoPorCodigo,
    VerificandoPedidoPorCliente,
    VerificandoPedidoCpfCnpj,
    VerificandoPedidoSelecionarCliente,
    VerificandoPedidoListarPedidos,
    VerificandoPedidoSelecionarPedido,
    Encerrado
}

