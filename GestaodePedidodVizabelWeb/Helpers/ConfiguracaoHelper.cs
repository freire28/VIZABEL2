using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Data;
using GestaoPedidosVizabel.Models;

namespace GestaoPedidosVizabel.Helpers
{
    public static class ConfiguracaoHelper
    {
        /// <summary>
        /// Busca o valor de uma configuração pela chave
        /// </summary>
        /// <param name="context">Contexto do banco de dados</param>
        /// <param name="chave">Chave da configuração</param>
        /// <param name="apenasAtivas">Se true, busca apenas configurações ativas (padrão: true)</param>
        /// <returns>Valor da configuração ou null se não encontrada</returns>
        public static async Task<string?> BuscarValorConfiguracaoAsync(
            ApplicationDbContext context, 
            string chave, 
            bool apenasAtivas = true)
        {
            if (string.IsNullOrWhiteSpace(chave))
                return null;

            var query = context.Configuracoes.Where(c => c.Chave == chave);
            
            if (apenasAtivas)
            {
                query = query.Where(c => c.Ativo);
            }

            var configuracao = await query.FirstOrDefaultAsync();
            
            return configuracao?.Valor;
        }

        /// <summary>
        /// Busca uma configuração completa pela chave
        /// </summary>
        /// <param name="context">Contexto do banco de dados</param>
        /// <param name="chave">Chave da configuração</param>
        /// <param name="apenasAtivas">Se true, busca apenas configurações ativas (padrão: true)</param>
        /// <returns>Objeto Configuracao ou null se não encontrada</returns>
        public static async Task<Configuracao?> BuscarConfiguracaoAsync(
            ApplicationDbContext context, 
            string chave, 
            bool apenasAtivas = true)
        {
            if (string.IsNullOrWhiteSpace(chave))
                return null;

            var query = context.Configuracoes.Where(c => c.Chave == chave);
            
            if (apenasAtivas)
            {
                query = query.Where(c => c.Ativo);
            }

            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Busca o valor de uma configuração como inteiro
        /// </summary>
        /// <param name="context">Contexto do banco de dados</param>
        /// <param name="chave">Chave da configuração</param>
        /// <param name="valorPadrao">Valor padrão caso não encontre ou não consiga converter</param>
        /// <param name="apenasAtivas">Se true, busca apenas configurações ativas (padrão: true)</param>
        /// <returns>Valor inteiro da configuração ou valorPadrao</returns>
        public static async Task<int> BuscarValorIntConfiguracaoAsync(
            ApplicationDbContext context, 
            string chave, 
            int valorPadrao = 0,
            bool apenasAtivas = true)
        {
            var valor = await BuscarValorConfiguracaoAsync(context, chave, apenasAtivas);
            
            if (string.IsNullOrWhiteSpace(valor))
                return valorPadrao;

            if (int.TryParse(valor, out var resultado))
                return resultado;

            return valorPadrao;
        }

        /// <summary>
        /// Busca o valor de uma configuração como decimal
        /// </summary>
        /// <param name="context">Contexto do banco de dados</param>
        /// <param name="chave">Chave da configuração</param>
        /// <param name="valorPadrao">Valor padrão caso não encontre ou não consiga converter</param>
        /// <param name="apenasAtivas">Se true, busca apenas configurações ativas (padrão: true)</param>
        /// <returns>Valor decimal da configuração ou valorPadrao</returns>
        public static async Task<decimal> BuscarValorDecimalConfiguracaoAsync(
            ApplicationDbContext context, 
            string chave, 
            decimal valorPadrao = 0,
            bool apenasAtivas = true)
        {
            var valor = await BuscarValorConfiguracaoAsync(context, chave, apenasAtivas);
            
            if (string.IsNullOrWhiteSpace(valor))
                return valorPadrao;

            if (decimal.TryParse(valor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var resultado))
                return resultado;

            return valorPadrao;
        }

        /// <summary>
        /// Busca o valor de uma configuração como booleano
        /// </summary>
        /// <param name="context">Contexto do banco de dados</param>
        /// <param name="chave">Chave da configuração</param>
        /// <param name="valorPadrao">Valor padrão caso não encontre ou não consiga converter</param>
        /// <param name="apenasAtivas">Se true, busca apenas configurações ativas (padrão: true)</param>
        /// <returns>Valor booleano da configuração ou valorPadrao</returns>
        public static async Task<bool> BuscarValorBoolConfiguracaoAsync(
            ApplicationDbContext context, 
            string chave, 
            bool valorPadrao = false,
            bool apenasAtivas = true)
        {
            var valor = await BuscarValorConfiguracaoAsync(context, chave, apenasAtivas);
            
            if (string.IsNullOrWhiteSpace(valor))
                return valorPadrao;

            if (bool.TryParse(valor, out var resultado))
                return resultado;

            // Também aceita "1" ou "0" como valores booleanos
            if (valor == "1" || valor.ToLower() == "true" || valor.ToLower() == "sim")
                return true;
            
            if (valor == "0" || valor.ToLower() == "false" || valor.ToLower() == "não" || valor.ToLower() == "nao")
                return false;

            return valorPadrao;
        }

        /// <summary>
        /// Obtém a descrição do regime tributário baseado no valor numérico
        /// </summary>
        /// <param name="valor">Valor numérico do regime tributário</param>
        /// <returns>Descrição do regime tributário</returns>
        public static string ObterDescricaoRegimeTributario(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            return valor switch
            {
                "1" => "1 - Simples Nacional",
                "2" => "2 - Simples Nacional - excesso de sublimite",
                "3" => "3 - Regime Normal",
                _ => valor
            };
        }
    }
}

