﻿public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public List<OrderItem> Items { get; set; }
}
