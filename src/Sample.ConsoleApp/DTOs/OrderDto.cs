public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();   
}
