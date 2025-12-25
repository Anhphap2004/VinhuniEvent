using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhuniEvent.Models
{
    [Table("Contacts")]
    public class Contact
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ContactId { get; set; }

        // FK -> Users.UserId
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string? Subject { get; set; }

        [Required]
        public string? Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}
