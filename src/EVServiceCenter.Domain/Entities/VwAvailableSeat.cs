using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class VwAvailableSeat
{
    public int SeatId { get; set; }

    public string SeatNumber { get; set; }

    public string RowLabel { get; set; }

    public int? ColumnNumber { get; set; }

    public string SeatType { get; set; }

    public int ShowtimeId { get; set; }

    public int? MovieId { get; set; }

    public int? CinemaRoomId { get; set; }

    public DateOnly? ShowDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public string Status { get; set; }
}
