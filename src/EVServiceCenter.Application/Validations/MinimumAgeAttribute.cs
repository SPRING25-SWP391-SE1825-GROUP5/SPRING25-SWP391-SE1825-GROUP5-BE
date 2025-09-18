using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Validations
{
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
            ErrorMessage = $"Phải đủ {_minimumAge} tuổi trở lên";
        }

        public override bool IsValid(object value)
        {
            if (value is DateOnly dateOfBirth)
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var age = CalculateAge(dateOfBirth, today);
                return age >= _minimumAge;
            }

            return false;
        }

        private static int CalculateAge(DateOnly birthDate, DateOnly today)
        {
            var age = today.Year - birthDate.Year;

            // Nếu chưa đến sinh nhật năm nay thì trừ đi 1 tuổi
            if (birthDate.Month > today.Month || 
                (birthDate.Month == today.Month && birthDate.Day > today.Day))
            {
                age--;
            }

            return age;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Phải đủ {_minimumAge} tuổi trở lên để đăng ký tài khoản";
        }
    }
}
