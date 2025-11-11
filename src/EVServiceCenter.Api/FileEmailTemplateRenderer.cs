using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace EVServiceCenter.Api
{
    public class FileEmailTemplateRenderer : IEmailTemplateRenderer
    {
        private readonly IWebHostEnvironment _env;
        private const string TemplatesRoot = "Templates/Emails";

        public FileEmailTemplateRenderer(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> RenderAsync(string templateName, IDictionary<string, string> placeholders)
        {
            var templatePath = Path.Combine(_env.ContentRootPath, TemplatesRoot, templateName + ".html");
            var html = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);

            if (placeholders != null)
            {
                // Xử lý conditional rendering TRƯỚC khi replace placeholder
                // {{#if fieldName}}...{{/if}} - hiển thị nếu fieldName không rỗng
                var conditionalPattern = @"{{#if\s+(\w+)}}(.*?){{/if}}";
                html = System.Text.RegularExpressions.Regex.Replace(html, conditionalPattern, (match) =>
                {
                    var fieldName = match.Groups[1].Value;
                    var content = match.Groups[2].Value;

                    // Kiểm tra xem field có giá trị không rỗng không
                    if (placeholders.ContainsKey(fieldName) && !string.IsNullOrWhiteSpace(placeholders[fieldName]))
                    {
                        // Hiển thị content (sẽ được replace placeholder sau)
                        return content;
                    }
                    else
                    {
                        // Ẩn content
                        return string.Empty;
                    }
                }, System.Text.RegularExpressions.RegexOptions.Singleline);

                // Xử lý logic có/không có discount (đặc biệt)
                if (placeholders.ContainsKey("hasDiscount"))
                {
                    bool hasDiscount = placeholders["hasDiscount"] == "true";
                    if (hasDiscount)
                    {
                        // Hiển thị phần discount
                        html = html.Replace("{{#if hasDiscount}}", "");
                        html = html.Replace("{{/if}}", "");
                    }
                    else
                    {
                        // Ẩn phần discount
                        html = System.Text.RegularExpressions.Regex.Replace(html, @"{{#if hasDiscount}}.*?{{/if}}", "", System.Text.RegularExpressions.RegexOptions.Singleline);
                    }
                }

                // Sau đó replace tất cả placeholder
                foreach (var kv in placeholders)
                {
                    html = html.Replace("{{" + kv.Key + "}}", kv.Value ?? string.Empty);
                }
            }

            return html;
        }
    }
}


