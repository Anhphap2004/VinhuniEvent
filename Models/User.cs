using System;
using System.Collections.Generic;

namespace VinhuniEvent.Models;

public partial class User
{
    public int UserId { get; set; }

        public string? StudentCode { get; set; }     
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public int RoleId { get; set; }

        public string? Faculty { get; set; }         
        public string? Major { get; set; }           
        public DateTime? BirthDate { get; set; }    
        public string? PhoneNumber { get; set; }     
        public string? ImageUrl { get; set; }       

    public DateTime? CreatedDate { get; set; }

    public string? StudentCode { get; set; }

    public string? Faculty { get; set; }

    public string? Major { get; set; }

    public DateTime? BirthDate { get; set; }

    public string? PhoneNumber { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<EventRegistration> EventRegistrations { get; set; } = new List<EventRegistration>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual Role? Role { get; set; }
}
