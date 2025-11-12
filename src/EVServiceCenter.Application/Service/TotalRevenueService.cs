using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class TotalRevenueService : ITotalRevenueService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPaymentRepository _paymentRepository;

        public TotalRevenueService(
            IInvoiceRepository invoiceRepository,
            IPaymentRepository paymentRepository)
        {
            _invoiceRepository = invoiceRepository;
            _paymentRepository = paymentRepository;
        }

        public async Task<TotalRevenueOverTimeResponse> GetTotalRevenueOverTimeAsync(TotalRevenueOverTimeRequest? request = null)
        {
            var fromDate = request?.FromDate ?? DateTime.Today.AddDays(-30);
            var toDate = request?.ToDate ?? DateTime.Today;
            var granularity = (request?.Granularity ?? "DAY").Trim().ToUpperInvariant();

            fromDate = fromDate.Date;
            toDate = toDate.Date.AddDays(1).AddTicks(-1);

            if (granularity != "DAY" && granularity != "MONTH" && granularity != "QUARTER" && granularity != "YEAR")
            {
                granularity = "DAY";
            }

            var periods = GeneratePeriods(fromDate, toDate, granularity);

            var includedStatuses = new[] { "COMPLETED", "PAID" };
            var completedPayments = await _paymentRepository.GetPaymentsByStatusesAndDateRangeAsync(includedStatuses, fromDate, toDate);

            decimal totalRevenue = 0;
            foreach (var period in periods)
            {
                var periodRevenue = completedPayments
                    .Where(p => p.PaidAt != null && p.PaidAt >= period.StartDate && p.PaidAt <= period.EndDate)
                    .Sum(p => p.Amount);

                period.Revenue = periodRevenue;
                totalRevenue += periodRevenue;
            }

            return new TotalRevenueOverTimeResponse
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow,
                FromDate = fromDate,
                ToDate = toDate,
                Granularity = granularity,
                TotalRevenue = totalRevenue,
                Periods = periods
            };
        }

        private static List<TotalRevenuePeriod> GeneratePeriods(DateTime from, DateTime to, string granularity)
        {
            var result = new List<TotalRevenuePeriod>();

            if (granularity == "DAY")
            {
                for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
                {
                    result.Add(new TotalRevenuePeriod
                    {
                        PeriodKey = d.ToString("yyyy-MM-dd"),
                        StartDate = d,
                        EndDate = d.AddDays(1).AddTicks(-1)
                    });
                }
            }
            else if (granularity == "MONTH")
            {
                var cursor = new DateTime(from.Year, from.Month, 1);
                var end = new DateTime(to.Year, to.Month, 1);
                while (cursor <= end)
                {
                    var start = cursor;
                    var endOfMonth = cursor.AddMonths(1).AddTicks(-1);
                    result.Add(new TotalRevenuePeriod
                    {
                        PeriodKey = cursor.ToString("yyyy-MM"),
                        StartDate = start,
                        EndDate = endOfMonth
                    });
                    cursor = cursor.AddMonths(1);
                }
            }
            else if (granularity == "QUARTER")
            {
                var startQuarter = GetQuarterStart(from);
                var endQuarterStart = GetQuarterStart(to);
                for (var q = startQuarter; q <= endQuarterStart; q = q.AddMonths(3))
                {
                    result.Add(new TotalRevenuePeriod
                    {
                        PeriodKey = $"{q:yyyy}-Q{GetQuarterNumber(q)}",
                        StartDate = q,
                        EndDate = q.AddMonths(3).AddTicks(-1)
                    });
                }
            }
            else // YEAR
            {
                for (var y = new DateTime(from.Year, 1, 1); y <= new DateTime(to.Year, 1, 1); y = y.AddYears(1))
                {
                    result.Add(new TotalRevenuePeriod
                    {
                        PeriodKey = y.ToString("yyyy"),
                        StartDate = y,
                        EndDate = y.AddYears(1).AddTicks(-1)
                    });
                }
            }

            foreach (var p in result)
            {
                if (p.StartDate < from) p.StartDate = from;
                if (p.EndDate > to) p.EndDate = to;
            }

            return result;
        }

        private static DateTime GetQuarterStart(DateTime dt)
        {
            var q = ((dt.Month - 1) / 3) * 3 + 1;
            return new DateTime(dt.Year, q, 1);
        }

        private static int GetQuarterNumber(DateTime dt)
        {
            return ((dt.Month - 1) / 3) + 1;
        }
    }
}