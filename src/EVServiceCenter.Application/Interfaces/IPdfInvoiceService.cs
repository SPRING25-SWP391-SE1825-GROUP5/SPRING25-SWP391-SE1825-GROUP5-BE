using System.Threading.Tasks;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IPdfInvoiceService
    {
        Task<byte[]> GenerateInvoicePdfAsync(int bookingId);
        Task<byte[]> GenerateMaintenanceReportPdfAsync(int bookingId);
    }
}
