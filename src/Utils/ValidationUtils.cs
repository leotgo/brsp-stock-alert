using System.Text.RegularExpressions;

namespace BRSP
{
    public static class ValidationUtils
    {
        public static bool IsValidB3StockCode(string stockCode)
        {
            string stockCodeRegex = @"^[A-Z0-9]{4}[A-Z0-9]{1,2}$";
            return Regex.IsMatch(stockCode, stockCodeRegex, RegexOptions.IgnoreCase);
        }

        public static bool IsValidEmailAddress(string emailAddress)
        {
            string emailRegex = @"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$";
            return Regex.IsMatch(emailAddress, emailRegex, RegexOptions.IgnoreCase);
        }
    }
}