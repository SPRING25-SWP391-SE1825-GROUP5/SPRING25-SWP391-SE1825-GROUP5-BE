using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models
{
    public class PaymentMethodRevenueResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PaymentMethodRevenueData? Data { get; set; }
    }

    public class PaymentMethodRevenueData
    {
        public int? CenterId { get; set; }
        public string? CenterName { get; set; }
        public PaymentMethodsInfo PaymentMethods { get; set; } = new PaymentMethodsInfo();
        public RevenueSummary Summary { get; set; } = new RevenueSummary();
        public DateRangeInfo? DateRange { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class PaymentMethodsInfo
    {
        public PaymentMethodDetail PAYOS { get; set; } = new PaymentMethodDetail();
        public PaymentMethodDetail CASH { get; set; } = new PaymentMethodDetail();
    }

    public class PaymentMethodDetail
    {
        public decimal TotalRevenue { get; set; }
        public int TransactionCount { get; set; }
        public decimal Percentage { get; set; }
        public decimal AverageTransactionValue { get; set; }
    }

    public class RevenueSummary
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransactionValue { get; set; }
    }
}
