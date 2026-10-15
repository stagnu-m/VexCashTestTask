using System.Globalization;
using System.Text.RegularExpressions;
using VexCash.SmsService.Models;

namespace VexCash.SmsService.Services
{
    public interface ITemplateRenderer
    {
        /// <summary>
        /// Renders a template string by replacing placeholders ({Creditnumber}, {Name}, {dueDate}, {amount})
        /// with actual values from the customer entity.
        /// </summary>
        string Render(string template, Customer customer, CultureInfo? culture = null);
    }

    public class TemplateRenderer : ITemplateRenderer
    {
        private static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo("de-DE");

        public string Render(string template, Customer customer, CultureInfo? culture = null)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            var cult = culture ?? DefaultCulture;

            var amountFormatted = $"{customer.Amount.ToString("N2", cult)} EUR";
            var dueDateFormatted = customer.DueDate.ToString("dd.MM.yyyy", cult);

            var result = template;

            result = Regex.Replace(result, @"\{\s*creditnumber\s*\}", customer.CreditNumber ?? string.Empty, RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\{\s*name\s*\}", customer.Name ?? string.Empty, RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\{\s*duedate\s*\}", dueDateFormatted, RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\{\s*amount\s*\}", amountFormatted, RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\{\s*phonenr\s*\}", customer.PhoneNumber ?? string.Empty, RegexOptions.IgnoreCase);

            return result;
        }
    }
}
