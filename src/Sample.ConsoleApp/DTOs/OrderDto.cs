﻿public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public List<OrderItemDto> Items { get; set; }
}
