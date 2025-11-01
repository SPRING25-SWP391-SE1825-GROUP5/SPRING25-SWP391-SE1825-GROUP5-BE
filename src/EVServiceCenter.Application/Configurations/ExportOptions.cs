using System;

namespace EVServiceCenter.Application.Configurations
{
    public class ExportOptions
    {
        public int MaxRecords { get; set; } = 100000;
        public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        public CsvOptions Csv { get; set; } = new CsvOptions();

        public class CsvOptions
        {
            public bool IncludeBom { get; set; } = true;
        }
    }
}


