using System.Collections.Generic;
using System.Threading.Tasks;

namespace EVServiceCenter.Application.Interfaces
{
    /// <summary>
    /// Renders email templates with placeholder replacement.
    /// </summary>
    public interface IEmailTemplateRenderer
    {
        Task<string> RenderAsync(string templateName, IDictionary<string, string> placeholders);
    }
}


