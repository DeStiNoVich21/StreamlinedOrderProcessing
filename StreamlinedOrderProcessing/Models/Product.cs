using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StreamlinedOrderProcessing.Models
{
    [Table("Product")]
    public class Product
    {
        [Key, Column("product_id")]
        public int ProductId { get; set; }

        [Column("title")]
        public string Title { get; set; } = null!;

        [Column("price")]
        public decimal Price { get; set; }

        [Column("stock_quantity")]
        public int StockQuantity { get; set; }

        [Column("description")]
        public string? Description { get; set; }
    }
}
