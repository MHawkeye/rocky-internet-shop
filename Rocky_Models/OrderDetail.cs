using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rocky_Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("OrderHeaderId")]
        public int OrderHeaderId { get; set; }
        public OrderHeader OrderHeader { get; set; }

        [ForeignKey("ProductId")]
        public int ProductId { get; set; }
        
        public Product Product { get; set; }
        public int Sqft { get; set; }
        public double PricePerSqFt { get; set; }
    }
}
