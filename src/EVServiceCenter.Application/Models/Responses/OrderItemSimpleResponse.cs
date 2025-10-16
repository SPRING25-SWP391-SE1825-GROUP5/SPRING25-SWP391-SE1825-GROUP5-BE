namespace EVServiceCenter.Application.Models.Responses
{
	public class OrderItemSimpleResponse
	{
		public int OrderItemId { get; set; }
		public int PartId { get; set; }
		public required string PartName { get; set; }
		public decimal UnitPrice { get; set; }
		public int Quantity { get; set; }
		public decimal Subtotal { get; set; }
	}
}
