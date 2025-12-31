using System;
using System.ComponentModel.DataAnnotations;

namespace VinhuniEvent.Models;

public class Certificate
{
    [Key]
    public int CertificateId { get; set; }

    public int EventId { get; set; }

    public int UserId { get; set; }

    [MaxLength(100)]
    public string CertificateCode { get; set; } = null!;

    public DateTime IssuedAt { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Issued";

    // Navigation properties
    public virtual Event? Event { get; set; }
    public virtual User? User { get; set; }
}
