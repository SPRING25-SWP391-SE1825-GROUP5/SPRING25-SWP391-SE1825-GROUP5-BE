using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EVServiceCenter.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EVServiceCenter.Application.Service
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary? _cloudinary;
        private readonly IConfiguration _configuration;

        public CloudinaryService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            try
            {
                var cloudName = _configuration["Cloudinary:CloudName"];
                var apiKey = _configuration["Cloudinary:ApiKey"];
                var apiSecret = _configuration["Cloudinary:ApiSecret"];

                // Debug logging
                // Cloudinary configuration loaded successfully

                if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                {
                    // Cloudinary configuration is missing - upload feature disabled
                    return;
                }

                var account = new Account(cloudName, apiKey, apiSecret);
                _cloudinary = new Cloudinary(account);
                // Cloudinary service initialized successfully
            }
            catch (Exception)
            {
                // Failed to initialize Cloudinary service - upload feature disabled
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder = "avatars")
        {
            try
            {
                if (_cloudinary == null)
                    throw new InvalidOperationException("Cloudinary service is not configured. Please check appsettings.json");

                // Validate file
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File không được để trống");

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    throw new ArgumentException("Định dạng file không được hỗ trợ. Vui lòng chọn file ảnh có định dạng JPG hoặc PNG.");
                }

                // Validate file size (max 4MB)
                if (file.Length > 4 * 1024 * 1024)
                {
                    var fileSizeMB = Math.Round(file.Length / (1024.0 * 1024.0), 2);
                    throw new ArgumentException($"Kích thước file quá lớn. File hiện tại: {fileSizeMB}MB, tối đa cho phép: 4MB.");
                }

                // Create unique public ID
                var publicId = $"{folder}/{Guid.NewGuid()}";

                // Upload to Cloudinary
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    PublicId = publicId,
                    Folder = folder,
                    Overwrite = false,
                    Transformation = new Transformation()
                        .Width(400)
                        .Height(400)
                        .Crop("fill")
                        .Gravity("face")
                        .Quality("auto")
                        .FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Upload failed: {uploadResult.Error?.Message}");
                }

                return uploadResult.SecureUrl.ToString();
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình upload ảnh: {ex.Message}");
            }
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            try
            {
                if (_cloudinary == null)
                    return false;

                if (string.IsNullOrEmpty(publicId))
                    return false;

                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch (Exception)
            {
                // Error deleting image - continuing without error
                return false;
            }
        }

        public Task<string> GetImageUrlAsync(string publicId, int? width = null, int? height = null)
        {
            try
            {
                if (_cloudinary == null)
                    return Task.FromResult(string.Empty);

                if (string.IsNullOrEmpty(publicId))
                    return Task.FromResult(string.Empty);

                var transformation = new Transformation();
                
                if (width.HasValue && height.HasValue)
                {
                    transformation = transformation.Width(width.Value).Height(height.Value).Crop("fill");
                }
                else if (width.HasValue)
                {
                    transformation = transformation.Width(width.Value).Crop("scale");
                }
                else if (height.HasValue)
                {
                    transformation = transformation.Height(height.Value).Crop("scale");
                }

                var url = _cloudinary.Api.UrlImgUp.Transform(transformation).BuildUrl(publicId);
                return Task.FromResult(url);
            }
            catch (Exception)
            {
                // Error getting image URL - returning null
                return Task.FromResult(string.Empty);
            }
        }
    }
}
