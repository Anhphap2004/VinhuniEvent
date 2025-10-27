using System;
using System.Collections.Generic;

namespace VinhuniEvent.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int EventId { get; set; }

    public int UserId { get; set; }

    public DateTime? AttendanceTime { get; set; }

    public bool? IsPresent { get; set; }

    public virtual Event? Event { get; set; }

    public virtual User? User { get; set; } 
}
