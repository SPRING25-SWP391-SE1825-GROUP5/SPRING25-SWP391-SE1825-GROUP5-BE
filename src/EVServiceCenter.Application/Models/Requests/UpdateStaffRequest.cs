using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateStaffRequest
    {
        public bool? IsActive { get; set; }
    }
}
