using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
	public class CreateInventoryRequest
	{
		[Required(ErrorMessage = "ID trung tâm là bắt buộc")]
		[Range(1, int.MaxValue, ErrorMessage = "ID trung tâm không hợp lệ")]
		public int CenterId { get; set; }
	}
}
