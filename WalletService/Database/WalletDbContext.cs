using Microsoft.EntityFrameworkCore;
using WalletService.Database.Models;

namespace WalletService.Models
{
    public class WalletDbContext : DbContext
    {
        public DbSet<TransactionRecords> Transactions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlServer(
                "Data Source=localhost, 1433;"+
                "Persist Security Info=True;"+
                "User ID=sa;"+
                "Password=Password01;"+
                "Database=WalletDb"
                );
    }
}
