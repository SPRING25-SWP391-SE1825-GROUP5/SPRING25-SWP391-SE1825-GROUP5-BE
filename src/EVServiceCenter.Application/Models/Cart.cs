using System;
using System.Collections.Generic;
using System.Linq;

namespace EVServiceCenter.Application.Models;

public class Cart
{
    public int CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CartItem> Items { get; set; } = new List<CartItem>();

    public decimal TotalAmount => Items.Sum(item => item.Subtotal);
    public int ItemCount => Items.Sum(item => item.Quantity);
    public int UniqueItemCount => Items.Count;
}

