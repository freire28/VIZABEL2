using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GestaoPedidosVizabel.Models.Validacoes
{
    public class NcmAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("O campo NCM é obrigatório.");
            }

            var ncm = value.ToString()!;

            // NCM deve ter exatamente 8 dígitos numéricos
            if (!Regex.IsMatch(ncm, @"^\d{8}$"))
            {
                return new ValidationResult("O NCM deve conter exatamente 8 dígitos numéricos.");
            }

            return ValidationResult.Success;
        }
    }
}









