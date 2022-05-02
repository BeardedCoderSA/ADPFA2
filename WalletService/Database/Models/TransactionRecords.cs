using System.ComponentModel.DataAnnotations;

namespace WalletService.Database.Models
{
    public class TransactionRecords
    {
        [Key]
        public long Id { get; set; }
        public string Symbol { get; set; } 
        public string Transaction_Type { get; set; }
        public decimal Qty { get; set; } = 0;
    }
}
