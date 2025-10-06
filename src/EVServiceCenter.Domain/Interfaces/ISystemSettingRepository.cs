using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces;

public interface ISystemSettingRepository
{
    Task<SystemSetting?> GetAsync(string key);
    Task<IDictionary<string, SystemSetting>> GetManyAsync(IEnumerable<string> keys);
    Task UpsertAsync(string key, string value, string? description = null);
}


