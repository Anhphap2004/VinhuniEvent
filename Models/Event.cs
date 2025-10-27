using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
namespace VinhuniEvent.Models;

public partial class Event
{
    public int EventId { get; set; }

    public int CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Location { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int? CreatedBy { get; set; }

    public int? MaxParticipants { get; set; }

    public string? Image { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? Status { get; set; }

    public DateTime? RegistrationDeadline { get; set; }

    public int? CurrentParticipants { get; set; }

    public string? ApprovalStatus { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public int? UpdatedBy { get; set; }

    public string? Note { get; set; }

    public string? Slug { get; set; }

    [NotMapped]
    public IFormFile? ImageFile { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual EventCategory? Category { get; set; } 

    public virtual User? CreatedByNavigation { get; set; } 

    public virtual ICollection<EventRegistration> EventRegistrations { get; set; } = new List<EventRegistration>();
}
