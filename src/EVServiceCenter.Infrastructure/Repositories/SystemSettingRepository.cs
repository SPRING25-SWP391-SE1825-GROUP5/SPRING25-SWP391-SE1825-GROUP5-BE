using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories;

public class SystemSettingRepository : ISystemSettingRepository
{
    private readonly EVDbContext _db;

    public SystemSettingRepository(EVDbContext db)
    {
        _db = db;
    }

    public async Task<SystemSetting?> GetAsync(string key)
    {
        return await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.SettingKey == key);
    }

    public async Task<IDictionary<string, SystemSetting>> GetManyAsync(IEnumerable<string> keys)
    {
        var list = await _db.SystemSettings.AsNoTracking().Where(s => keys.Contains(s.SettingKey)).ToListAsync();
        return list.ToDictionary(s => s.SettingKey, s => s);
    }

    public async Task UpsertAsync(string key, string value, string? description = null)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        if (setting == null)
        {
            setting = new SystemSetting
            {
                SettingKey = key,
                SettingValue = value,
                Description = description ?? string.Empty,
                UpdatedAt = System.DateTime.UtcNow
            };
            _db.SystemSettings.Add(setting);
        }
        else
        {
            setting.SettingValue = value;
            if (description != null) setting.Description = description;
            setting.UpdatedAt = System.DateTime.UtcNow;
            _db.SystemSettings.Update(setting);
        }
        await _db.SaveChangesAsync();
    }
}


