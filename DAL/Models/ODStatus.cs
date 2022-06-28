using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class ODStatus
    {
        [Key]
        public Guid ODId { get; set; }

        public Guid OrderId { get; set; }

        public Guid UserId { get; set; }

        public bool Delivered { get; set; }

        public int OrderPrice { get; set; }
        public DateTime CreatedDate { get; set; }

        public ODStatus()
        {
            CreatedDate = DateTime.Now;
        }
    }
}
