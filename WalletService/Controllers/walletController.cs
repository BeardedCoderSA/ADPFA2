using Microsoft.AspNetCore.Mvc;
using WalletService.Database.Models;
using WalletService.DTO;
using WalletService.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Data;

namespace WalletService.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class walletController : Controller
    {
        public readonly WalletDbContext _dbContext;
        private readonly IServiceScopeFactory _scopeFactory;

        public walletController(WalletDbContext dbContext, IServiceScopeFactory scopeFactory)
        {
            _dbContext = dbContext;
            _scopeFactory = scopeFactory;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            List<TransactionRecords> transList = await _dbContext.Transactions.ToListAsync();

            return View(transList);
        }

        [HttpPost]
        public async Task<IActionResult> Deposit([FromBody] walletItems items)
        {
            if (items.Symbol == ".XBT" || items.Symbol == ".BETH" || items.Symbol == ".BADAT")
            {
                TransactionRecords transactionRec = new TransactionRecords
                {
                    Symbol = items.Symbol,
                    Transaction_Type = "Deposit",
                    Qty = (decimal)items.Qty,
                };

                using IServiceScope? scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetService<WalletDbContext>();

                _dbContext.Transactions.Add(transactionRec);
                _dbContext.SaveChanges();
                return Ok("Deposit Succesfull!");
            } else
            {
                return Ok("You can only deposit .XBT/.BETH/.BADAT");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Withdraw([FromBody] walletItems items)
        {
            if (items.Symbol == ".XBT" || items.Symbol == ".BETH" || items.Symbol == ".BADAT")
            {
                decimal Quantity = 0;
                List<TransactionRecords> transactionRecords = await _dbContext.Transactions.ToListAsync();
                foreach (TransactionRecords transactionRec in transactionRecords)
                {
                    if (transactionRec.Symbol == items.Symbol)
                    {
                        Quantity += transactionRec.Qty;
                    }
                }
                if (Quantity - (decimal)items.Qty >= 0)
                {
                    TransactionRecords rec = new TransactionRecords
                    {
                        Symbol = items.Symbol,
                        Transaction_Type = "Withdraw",
                        Qty = (decimal)items.Qty * -1,
                    };

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetService<WalletDbContext>();

                    _dbContext.Transactions.Add(rec);
                    _dbContext.SaveChanges();
                    return Ok("Withdrawal Succesfull!");
                }
                else
                {
                    return Ok("insufficient Funds");
                }
            } else
            {
                return Ok("You can only withdraw Bitcoin(.XBT), Ethereum(.BETH) and Cardano(.BADAT)");
            }
        }
    }
}
