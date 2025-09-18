using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EVServiceCenter.Application.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder = "avatars");
        Task<bool> DeleteImageAsync(string publicId);
        Task<string> GetImageUrlAsync(string publicId, int? width = null, int? height = null);
    }
}
