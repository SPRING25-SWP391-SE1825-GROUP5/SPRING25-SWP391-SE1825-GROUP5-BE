namespace EVServiceCenter.Application.Models.Responses
{
	public class OrderItemSimpleResponse
	{
		public int OrderItemId { get; set; }
		public int PartId { get; set; }
		public required string PartName { get; set; }
		public decimal UnitPrice { get; set; }
		public int Quantity { get; set; }
		public int ConsumedQty { get; set; } // Số lượng đã dùng
		public int AvailableQty => Quantity - ConsumedQty; // Số lượng còn lại có thể dùng
		public decimal Subtotal { get; set; }
	}
}
