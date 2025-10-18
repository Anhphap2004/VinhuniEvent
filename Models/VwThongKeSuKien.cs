using System;
using System.Collections.Generic;

namespace VinhuniEvent.Models;

public partial class VwThongKeSuKien
{
    public int EventId { get; set; }

    public string Title { get; set; } = null!;

    public string? CategoryName { get; set; }

    public int? SoNguoiDangKy { get; set; }

    public int? SoNguoiDiemDanh { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }
}
