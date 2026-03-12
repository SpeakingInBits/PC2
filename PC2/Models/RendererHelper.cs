using System.Text;
using System.Text.RegularExpressions;

namespace PC2.Models
{
    public static partial class RendererHelper
    {
        // Source-generated at compile time — zero allocation per call
        [GeneratedRegex(@"\(?\d{3}\)?[\s\-.]?\d{3}[\s\-.]?\d{4}")]
        private static partial Regex PhoneNumberRegex();

        [GeneratedRegex(@"[^\d]")]
        private static partial Regex NonDigitRegex();

        /// <summary>
        /// This method parses out all of the phone numbers from the given string 
        /// and returns a formatted string and all the phone numbers are wrapped in anchor tag
        /// </summary>
        /// <param name="complexPhoneNumber"></param>
        /// <returns></returns>
        public static string RenderPhone(string complexPhoneNumber)
        {
            if (string.IsNullOrWhiteSpace(complexPhoneNumber))
                return string.Empty;

            var sb = new StringBuilder();

            int lastIndex = 0;
            foreach (Match match in PhoneNumberRegex().Matches(complexPhoneNumber))
            {
                if (match.Index > lastIndex)
                    sb.Append(complexPhoneNumber, lastIndex, match.Index - lastIndex);

                string phone = match.Value;
                string tel = NonDigitRegex().Replace(phone, "");
                sb.Append($"<a href=\"tel:{tel}\">{phone}</a><br>");
                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < complexPhoneNumber.Length)
                sb.Append(complexPhoneNumber, lastIndex, complexPhoneNumber.Length - lastIndex);

            return sb.ToString();
        }
    }
}
