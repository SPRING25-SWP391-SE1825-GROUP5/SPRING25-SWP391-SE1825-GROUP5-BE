using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class QuickCreateCustomerRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public required string FullName { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 8)]
        public required string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public required string Email { get; set; }
    }
}


