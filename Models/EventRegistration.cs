using System;
using System.Collections.Generic;

namespace VinhuniEvent.Models;

public partial class EventRegistration
{
    public int RegistrationId { get; set; }

    public int EventId { get; set; }

    public int UserId { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public string? Status { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
