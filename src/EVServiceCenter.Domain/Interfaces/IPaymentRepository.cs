using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByPayOsOrderCodeAsync(long payOsOrderCode);
        Task<Payment> CreateAsync(Payment payment);
        Task UpdateAsync(Payment payment);
    }
}


