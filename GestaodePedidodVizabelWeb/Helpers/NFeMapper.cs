using GestaoPedidosVizabel.Models;
using GestaoPedidosVizabel.Models.ViewModels;

namespace GestaoPedidosVizabel.Helpers
{
    public static class NFeMapper
    {
        public static NFeEditViewModel ToEditViewModel(NFe nfe)
        {
            var viewModel = new NFeEditViewModel
            {
                IdNfe = nfe.IdNfe,
                IdEmpresa = nfe.IdEmpresa,
                Modelo = nfe.Modelo,
                Serie = nfe.Serie,
                Numero = nfe.Numero,
                NaturezaOperacao = nfe.NaturezaOperacao,
                DataEmissao = nfe.DataEmissao,
                TipoNfe = nfe.TipoNfe,
                Finalidade = nfe.Finalidade,
                ValorProdutos = nfe.ValorProdutos,
                ValorTotalNfe = nfe.ValorTotalNfe,
                Status = nfe.Status,
                ChaveAcesso = nfe.ChaveAcesso,
                DataSaida = nfe.DataSaida,
                RegimeTributario = nfe.RegimeTributario,
                IdPedido = nfe.IdPedido,
                EmitirNfe = nfe.Pedido?.EmitirNfe,
                StatusPedido = nfe.Pedido?.StatusPedido != null ? new StatusPedidoViewModel
                {
                    IdStatuspedido = nfe.Pedido.StatusPedido.IdStatuspedido,
                    Descricao = nfe.Pedido.StatusPedido.Descricao
                } : null
            };

            // Mapear destinatário
            if (nfe.Destinatario != null)
            {
                viewModel.Destinatario = new NFeDestinatarioViewModel
                {
                    Nome = nfe.Destinatario.Nome,
                    CnpjCpf = nfe.Destinatario.CnpjCpf,
                    IndIeDest = nfe.Destinatario.IndIeDest,
                    Ie = nfe.Destinatario.Ie,
                    Logradouro = nfe.Destinatario.Logradouro,
                    Numero = nfe.Destinatario.Numero,
                    Bairro = nfe.Destinatario.Bairro,
                    CodMun = nfe.Destinatario.CodMun,
                    Municipio = nfe.Destinatario.Municipio,
                    Uf = nfe.Destinatario.Uf,
                    Cep = nfe.Destinatario.Cep
                };
            }

            // Mapear itens
            if (nfe.Itens != null && nfe.Itens.Any())
            {
                viewModel.Itens = nfe.Itens.Select(item => new NFeItemEditViewModel
                {
                    IdItem = item.IdItem,
                    IdNfe = item.IdNfe,
                    CodProduto = item.CodProduto,
                    Descricao = item.Descricao,
                    Ncm = item.Ncm,
                    Cfop = item.Cfop,
                    Unidade = item.Unidade,
                    Quantidade = item.Quantidade,
                    ValorUnitario = item.ValorUnitario,
                    ValorTotal = item.ValorTotal,
                    Imposto = item.Imposto != null ? new NFeItemImpostoViewModel
                    {
                        IdImposto = item.Imposto.IdImposto,
                        IdItem = item.Imposto.IdItem,
                        Origem = item.Imposto.Origem,
                        CstCsosn = item.Imposto.CstCsosn,
                        BaseIcms = item.Imposto.BaseIcms,
                        AliquotaIcms = item.Imposto.AliquotaIcms,
                        ValorIcms = item.Imposto.ValorIcms,
                        BasePis = item.Imposto.BasePis,
                        ValorPis = item.Imposto.ValorPis,
                        BaseCofins = item.Imposto.BaseCofins,
                        ValorCofins = item.Imposto.ValorCofins
                    } : null
                }).ToList();
            }

            // Mapear pagamentos
            if (nfe.Pagamentos != null && nfe.Pagamentos.Any())
            {
                viewModel.Pagamentos = nfe.Pagamentos.Select(pag => new NFePagamentoViewModel
                {
                    IdPagamento = pag.IdPagamento,
                    TipoPagamento = pag.TipoPagamento,
                    ValorPago = pag.ValorPago
                }).ToList();
            }

            return viewModel;
        }
    }
}

