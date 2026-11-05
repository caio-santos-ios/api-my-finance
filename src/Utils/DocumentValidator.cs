using System.Text.RegularExpressions;

namespace api_finances.src.Utils
{
    public static class DocumentValidator
    {
        public static bool IsValidCpf(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return false;
            string digits = Regex.Replace(raw, @"\D", "");
            if (digits.Length != 11) return false;

            if (new string(digits[0], 11) == digits) return false;

            int[] mult1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
            int sum = 0;
            for (int i = 0; i < 9; i++) sum += (digits[i] - '0') * mult1[i];
            int rem = (sum * 10) % 11;
            if (rem == 10) rem = 0;
            if (rem != (digits[9] - '0')) return false;

            int[] mult2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];
            sum = 0;
            for (int i = 0; i < 10; i++) sum += (digits[i] - '0') * mult2[i];
            rem = (sum * 10) % 11;
            if (rem == 10) rem = 0;
            return rem == (digits[10] - '0');
        }

        public static bool IsValidCnpj(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return false;
            string digits = Regex.Replace(raw, @"\D", "");
            if (digits.Length != 14) return false;

            if (new string(digits[0], 14) == digits) return false;

            int[] mult1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            int sum = 0;
            for (int i = 0; i < 12; i++) sum += (digits[i] - '0') * mult1[i];
            int rem = sum % 11;
            int digit1 = rem < 2 ? 0 : 11 - rem;
            if (digit1 != (digits[12] - '0')) return false;

            int[] mult2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            sum = 0;
            for (int i = 0; i < 13; i++) sum += (digits[i] - '0') * mult2[i];
            rem = sum % 11;
            int digit2 = rem < 2 ? 0 : 11 - rem;
            return digit2 == (digits[13] - '0');
        }

        public static bool IsValidCpfOrCnpj(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return true;
            string digits = Regex.Replace(raw, @"\D", "");
            if (digits.Length == 11) return IsValidCpf(digits);
            if (digits.Length == 14) return IsValidCnpj(digits);
            return false;
        }
    }
}
