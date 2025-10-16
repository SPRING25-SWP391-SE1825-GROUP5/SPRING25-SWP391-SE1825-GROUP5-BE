using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace EVServiceCenter.Application.Service
{
    public interface ILoginLockoutService
    {
        Task<bool> IsAccountLockedAsync(string email);
        Task RecordFailedAttemptAsync(string email);
        Task ClearFailedAttemptsAsync(string email);
        Task<LoginLockoutConfigResponse> GetConfigAsync();
        Task UpdateConfigAsync(LoginLockoutConfigRequest request);
        Task<int> GetRemainingAttemptsAsync(string email);
        Task<DateTime?> GetLockoutExpiryAsync(string email);
    }

    public class LoginLockoutService : ILoginLockoutService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;
        private const string CONFIG_CACHE_KEY = "login_lockout_config";
        private const string LOCKOUT_DATA_FILE = "lockout_data.json";
        private readonly string _dataFilePath;

        public LoginLockoutService(IMemoryCache memoryCache, IConfiguration configuration)
        {
            _memoryCache = memoryCache;
            _configuration = configuration;
            _dataFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", LOCKOUT_DATA_FILE);
            
            // Tạo thư mục Data nếu chưa có
            var dataDir = Path.GetDirectoryName(_dataFilePath);
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir ?? string.Empty);
            }
        }

        private string GetFailedAttemptsKey(string email) => $"login_lockout_{email.ToLower()}:failed_attempts";
        private string GetLockoutKey(string email) => $"login_lockout_{email.ToLower()}:lockout";

        private async Task<Dictionary<string, LockoutData>> LoadLockoutDataAsync()
        {
            if (!File.Exists(_dataFilePath))
            {
                return new Dictionary<string, LockoutData>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_dataFilePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, LockoutData>>(json);
                return data ?? new Dictionary<string, LockoutData>();
            }
            catch
            {
                return new Dictionary<string, LockoutData>();
            }
        }

        private async Task SaveLockoutDataAsync(Dictionary<string, LockoutData> data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_dataFilePath, json);
            }
            catch
            {
                // Log error if needed
            }
        }

        public async Task<bool> IsAccountLockedAsync(string email)
        {
            var config = await GetConfigAsync();
            if (!config.Enabled) return false;

            // Kiểm tra memory cache trước
            var lockoutKey = GetLockoutKey(email);
            var memoryLockout = _memoryCache.Get<string>(lockoutKey);
            if (!string.IsNullOrEmpty(memoryLockout))
            {
                var lockoutExpiry = JsonSerializer.Deserialize<DateTime>(memoryLockout);
                if (lockoutExpiry > DateTime.UtcNow)
                    return true;
            }

            // Kiểm tra file storage
            var data = await LoadLockoutDataAsync();
            var emailKey = email.ToLower();
            
            if (data.ContainsKey(emailKey))
            {
                var lockoutData = data[emailKey];
                
                // Nếu đã hết hạn, xóa khỏi file
                if (lockoutData.LockoutUntil <= DateTime.UtcNow)
                {
                    data.Remove(emailKey);
                    await SaveLockoutDataAsync(data);
                    return false;
                }
                
                return lockoutData.FailedAttempts >= config.MaxFailedAttempts;
            }

            return false;
        }

        public async Task RecordFailedAttemptAsync(string email)
        {
            var config = await GetConfigAsync();
            if (!config.Enabled) return;

            var emailKey = email.ToLower();
            var data = await LoadLockoutDataAsync();
            
            if (!data.ContainsKey(emailKey))
            {
                data[emailKey] = new LockoutData
                {
                    Email = email,
                    FailedAttempts = 0,
                    FirstFailedAttempt = DateTime.UtcNow,
                    LockoutUntil = null
                };
            }

            var lockoutData = data[emailKey];
            lockoutData.FailedAttempts++;
            
            // Lưu vào memory cache
            var failedKey = GetFailedAttemptsKey(email);
            _memoryCache.Set(failedKey, lockoutData.FailedAttempts, TimeSpan.FromMinutes(config.LockoutDurationMinutes * 2));

            // Nếu đạt số lần thất bại tối đa, bắt đầu lockout
            if (lockoutData.FailedAttempts >= config.MaxFailedAttempts)
            {
                lockoutData.LockoutUntil = DateTime.UtcNow.AddMinutes(config.LockoutDurationMinutes);
                
                // Lưu vào memory cache
                var lockoutKey = GetLockoutKey(email);
                var lockoutJson = JsonSerializer.Serialize(lockoutData.LockoutUntil);
                _memoryCache.Set(lockoutKey, lockoutJson, TimeSpan.FromMinutes(config.LockoutDurationMinutes));
            }

            // Lưu vào file
            await SaveLockoutDataAsync(data);
        }

        public async Task ClearFailedAttemptsAsync(string email)
        {
            var config = await GetConfigAsync();
            if (!config.Enabled) return;

            // Xóa khỏi memory cache
            _memoryCache.Remove(GetFailedAttemptsKey(email));
            _memoryCache.Remove(GetLockoutKey(email));

            // Xóa khỏi file
            var data = await LoadLockoutDataAsync();
            var emailKey = email.ToLower();
            
            if (data.ContainsKey(emailKey))
            {
                data.Remove(emailKey);
                await SaveLockoutDataAsync(data);
            }
        }

        public async Task<int> GetRemainingAttemptsAsync(string email)
        {
            var config = await GetConfigAsync();
            if (!config.Enabled) return config.MaxFailedAttempts;

            if (await IsAccountLockedAsync(email)) return 0;

            var data = await LoadLockoutDataAsync();
            var emailKey = email.ToLower();
            
            if (!data.ContainsKey(emailKey))
                return config.MaxFailedAttempts;
                
            var currentAttempts = data[emailKey].FailedAttempts;
            return Math.Max(0, config.MaxFailedAttempts - currentAttempts);
        }

        public async Task<DateTime?> GetLockoutExpiryAsync(string email)
        {
            var config = await GetConfigAsync();
            if (!config.Enabled) return null;

            // Kiểm tra memory cache trước
            var lockoutKey = GetLockoutKey(email);
            var memoryLockout = _memoryCache.Get<string>(lockoutKey);
            if (!string.IsNullOrEmpty(memoryLockout))
            {
                return JsonSerializer.Deserialize<DateTime>(memoryLockout);
            }

            // Kiểm tra file storage
            var data = await LoadLockoutDataAsync();
            var emailKey = email.ToLower();
            
            if (data.ContainsKey(emailKey))
            {
                return data[emailKey].LockoutUntil;
            }

            return null;
        }

        public Task<LoginLockoutConfigResponse> GetConfigAsync()
        {
            // Thử lấy từ memory cache trước
            var cachedConfig = _memoryCache.Get<LoginLockoutConfigResponse>(CONFIG_CACHE_KEY);
            if (cachedConfig != null) return Task.FromResult(cachedConfig);

            // Nếu không có trong cache, tạo từ configuration
            var config = new LoginLockoutConfigResponse
            {
                MaxFailedAttempts = int.TryParse(_configuration["LoginLockout:MaxFailedAttempts"], out var maxAttempts) ? maxAttempts : 5,
                LockoutDurationMinutes = int.TryParse(_configuration["LoginLockout:LockoutDurationMinutes"], out var duration) ? duration : 30,
                CacheKeyPrefix = _configuration["LoginLockout:CacheKeyPrefix"] ?? "login_lockout_",
                Enabled = bool.TryParse(_configuration["LoginLockout:Enabled"], out var enabled) ? enabled : true,
                LastUpdated = DateTime.UtcNow
            };

            // Lưu vào memory cache
            _memoryCache.Set(CONFIG_CACHE_KEY, config, TimeSpan.FromHours(1));
            return Task.FromResult(config);
        }

        public Task UpdateConfigAsync(LoginLockoutConfigRequest request)
        {
            var config = new LoginLockoutConfigResponse
            {
                MaxFailedAttempts = request.MaxFailedAttempts,
                LockoutDurationMinutes = request.LockoutDurationMinutes,
                CacheKeyPrefix = request.CacheKeyPrefix,
                Enabled = request.Enabled,
                LastUpdated = DateTime.UtcNow
            };

            // Cập nhật memory cache
            _memoryCache.Remove(CONFIG_CACHE_KEY);
            _memoryCache.Set(CONFIG_CACHE_KEY, config, TimeSpan.FromHours(1));
            return Task.CompletedTask;
        }
    }

    public class LockoutData
    {
        public string Email { get; set; } = string.Empty;
        public int FailedAttempts { get; set; }
        public DateTime FirstFailedAttempt { get; set; }
        public DateTime? LockoutUntil { get; set; }
    }
}
