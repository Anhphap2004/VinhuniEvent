using System;
using System.Collections.Generic;

namespace VinhuniEvent.Models
{
    public partial class User
    {
        public int UserId { get; set; }

        public string? StudentCode { get; set; }     // Mã sinh viên (có thể null)
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public int RoleId { get; set; }

        public string? Faculty { get; set; }         // Khoa
        public string? Major { get; set; }           // Ngành học
        public DateTime? BirthDate { get; set; }     // Ngày sinh
        public string? PhoneNumber { get; set; }     // Số điện thoại
        public string? ImageUrl { get; set; }        // Ảnh đại diện

        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }

        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual ICollection<EventRegistration> EventRegistrations { get; set; } = new List<EventRegistration>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual Role? Role { get; set; } 
    }
}
