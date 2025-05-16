using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PC2.Models
{
    public class RendererHelper
    {

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

                // Regex for US phone numbers: (xxx) xxx-xxxx, xxx-xxx-xxxx, xxx.xxx.xxxx, xxx xxx xxxx
                Regex phoneRegex = new Regex(@"\(?\d{3}\)?[\s\-.]?\d{3}[\s\-.]?\d{4}");
                var sb = new StringBuilder();

                int lastIndex = 0;
                foreach (Match match in phoneRegex.Matches(complexPhoneNumber))
                {
                    // Append any text before the match
                    if (match.Index > lastIndex)
                        sb.Append(complexPhoneNumber.Substring(lastIndex, match.Index - lastIndex));

                    string phone = match.Value;
                    // Remove formatting for tel: link
                    string tel = Regex.Replace(phone, @"[^\d]", "");
                    sb.Append($"<a href=\"tel:{tel}\">{phone}</a><br>");
                    lastIndex = match.Index + match.Length;
                }

                // Append any remaining text after the last match
                if (lastIndex < complexPhoneNumber.Length)
                    sb.Append(complexPhoneNumber.Substring(lastIndex));

                return sb.ToString();
            }
        }
}
