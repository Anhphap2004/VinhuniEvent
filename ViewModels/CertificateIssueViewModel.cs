using System;
using System.Collections.Generic;

namespace VinhuniEvent.ViewModels;

public class CertificateIssueViewModel
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime? EventDate { get; set; }
    public string? EventLocation { get; set; }
    public List<CertificateIssueItem> Items { get; set; } = new List<CertificateIssueItem>();
}

public class CertificateIssueItem
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
    public string? Faculty { get; set; }
    public bool IsPresent { get; set; }
    public bool AlreadyIssued { get; set; }
}
