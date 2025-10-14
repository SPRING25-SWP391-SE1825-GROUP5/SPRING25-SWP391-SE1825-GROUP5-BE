using System;

namespace EVServiceCenter.Domain.Entities;

public class MessageRead
{
    public long MessageId { get; set; }
    public int UserId { get; set; }
    public DateTime ReadAt { get; set; }

    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}


