using System;
using System.Collections.Generic;

namespace VinhuniEvent.Models;

public partial class RoleRequest
{
    public int RequestId { get; set; }

    public int UserId { get; set; }

    public string RequestedRole { get; set; } = null!;

    public string? Reason { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; } 
}
