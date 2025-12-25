using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhuniEvent.Models
{
    [Table("EventComments")]
    public class EventComment
    {
        [Key]
        public int CommentId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public int? ParentCommentId { get; set; }

        // Navigation
        public virtual Event? Event { get; set; } 
        public virtual User? User { get; set; } 
        public virtual EventComment? ParentComment { get; set; }
        public virtual ICollection<EventComment> Replies { get; set; } = new List<EventComment>();
    }
}
