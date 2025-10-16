using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EVServiceCenter.Application.Validations
{
    /// <summary>
    /// Email validator chặt chẽ hơn DataAnnotations EmailAddress.
    /// Chặn các trường hợp: chứa hai dấu chấm liên tiếp, bắt/đặc dấu chấm, domain không hợp lệ.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ValidEmailAttribute : ValidationAttribute
    {
        // Không cho phép ".." và yêu cầu domain có TLD >= 2 ký tự chữ
        private static readonly Regex EmailRegex = new Regex(
            "^(?!.*\\.\\.)[A-Za-z0-9](?:[A-Za-z0-9._%+-]{0,62}[A-Za-z0-9])?@" +
            "[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?(?:\\.[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public ValidEmailAttribute()
        {
            ErrorMessage = "Email không đúng định dạng";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return ValidationResult.Success; // để [Required] xử lý null
            }

            if (value is not string s)
            {
                return new ValidationResult(ErrorMessage);
            }

            s = s.Trim();
            if (s.Length > 254)
            {
                return new ValidationResult(ErrorMessage);
            }

            return EmailRegex.IsMatch(s) ? ValidationResult.Success : new ValidationResult(ErrorMessage);
        }
    }
}


