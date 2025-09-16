using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class SystemSetting
{
    public string SettingKey { get; set; }

    public string SettingValue { get; set; }

    public string Description { get; set; }

    public DateTime UpdatedAt { get; set; }
}
