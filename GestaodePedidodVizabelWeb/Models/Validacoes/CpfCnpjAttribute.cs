using System.ComponentModel.DataAnnotations;
using GestaoPedidosVizabel.Models;

namespace GestaoPedidosVizabel.Models.Validacoes
{
    public class CpfCnpjAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var cliente = validationContext.ObjectInstance as Cliente;
            
            if (cliente == null)
                return ValidationResult.Success;

            // Se for pessoa física (false), valida CPF
            if (!cliente.TipoPessoa) // false = Física
            {
                if (string.IsNullOrWhiteSpace(cliente.Cpfcnpj))
                {
                    return new ValidationResult("CPF é obrigatório para pessoa física.");
                }

                if (!ValidadorCPFCNPJ.ValidarCPF(cliente.Cpfcnpj))
                {
                    return new ValidationResult("CPF inválido.");
                }
            }
            // Se for pessoa jurídica (true), valida CNPJ
            else // true = Jurídica
            {
                if (string.IsNullOrWhiteSpace(cliente.Cpfcnpj))
                {
                    return new ValidationResult("CNPJ é obrigatório para pessoa jurídica.");
                }

                if (!ValidadorCPFCNPJ.ValidarCNPJ(cliente.Cpfcnpj))
                {
                    return new ValidationResult("CNPJ inválido.");
                }
            }

            return ValidationResult.Success;
        }
    }
}

