using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IPromotionService
    {
        Task<PromotionListResponse> GetAllPromotionsAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, string status = null, string promotionType = null);
        Task<PromotionResponse> GetPromotionByIdAsync(int promotionId);
        Task<PromotionResponse> GetPromotionByCodeAsync(string code);
        Task<PromotionResponse> CreatePromotionAsync(CreatePromotionRequest request);
        Task<PromotionResponse> UpdatePromotionAsync(int promotionId, UpdatePromotionRequest request);
        Task<bool> DeletePromotionAsync(int promotionId);
        Task<PromotionValidationResponse> ValidatePromotionAsync(ValidatePromotionRequest request);
        Task<bool> ActivatePromotionAsync(int promotionId);
        Task<bool> DeactivatePromotionAsync(int promotionId);
    }
}
