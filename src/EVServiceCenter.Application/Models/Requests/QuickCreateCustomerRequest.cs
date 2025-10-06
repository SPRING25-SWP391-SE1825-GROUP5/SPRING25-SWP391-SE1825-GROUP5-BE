using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class QuickCreateCustomerRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string FullName { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 8)]
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
    }
}


