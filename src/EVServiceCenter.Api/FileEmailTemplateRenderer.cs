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
                foreach (var kv in placeholders)
                {
                    html = html.Replace("{{" + kv.Key + "}}", kv.Value ?? string.Empty);
                }
                
                // Xử lý logic có/không có discount
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
            }

            return html;
        }
    }
}


