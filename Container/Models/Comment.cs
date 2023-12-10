using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace webleitour.Container.Models
{
    public class Comment
    {
        public DateTime CreatedDate { get; set; }
        public int CommentId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string UserPhoto { get; set; }
        public int PostId { get; set; }
        public string MessagePost { get; set; }
        public DateTime AlteratedDate { get; set; }
    }
}