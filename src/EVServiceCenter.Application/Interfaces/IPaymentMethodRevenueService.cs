using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IPaymentMethodRevenueService
    {
        /// <summary>
        /// Lấy doanh thu theo phương thức thanh toán
        /// </summary>
        /// <param name="centerId">ID của trung tâm (optional - null = tất cả center)</param>
        /// <param name="startDate">Ngày bắt đầu (optional)</param>
        /// <param name="endDate">Ngày kết thúc (optional)</param>
        /// <returns>Doanh thu theo phương thức thanh toán</returns>
        Task<PaymentMethodRevenueResponse> GetPaymentMethodRevenueAsync(
            int? centerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);
    }
}
