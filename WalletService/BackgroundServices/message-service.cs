using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WalletService.DTO;
using WalletService.Database.Models;
using WalletService.Models;
using System.Diagnostics;
using System.Runtime;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace WalletService.BackgroundServices
{
    public class message_service : BackgroundService
    {
        private ConnectionFactory? _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private const string queueName = "price-moved";

        public message_service(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _connectionFactory = new ConnectionFactory
            {
                UserName = "guest",
                Password = "guest"
            };
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queueName,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            _channel.BasicQos(0,1,false);

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var timer = new Timer(CheckMessages, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }
        private void CheckMessages(object state)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (sender, evnt) =>
            {
                var body = evnt.Body.ToArray();
                try
                {
                    var cryptoData = Encoding.UTF8.GetString(body);

                    string[] messageSplit = cryptoData.Split('.');
                    string movement = messageSplit[0];
                    string symbol = messageSplit[1];
                    string currency = "";
                    double totQty =0;
                    double curQty = 0;
                    double priceChange = 0.1;
                    double total = 0;

                    if (symbol == "BTC")
                    {
                        currency = ".XBT";
                    } 
                    else if (symbol == "ETH")
                    {
                        currency = ".BETH";
                    }
                    else if (symbol == "CAR")
                    {
                        currency = ".BADAT";
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetService<WalletDbContext>();

                    List<TransactionRecords> cryptoList = await dbContext.Transactions.ToListAsync();
                    foreach (TransactionRecords rec in cryptoList)
                    {
                        if (rec.Symbol == currency)
                        {
                            curQty = (double)rec.Qty;
                            totQty = (double)(totQty + curQty);
                            
                        }
                    }
                    Debug.WriteLine(totQty);

                    if (movement == "DOWN")
                    {
                        priceChange = -0.1;
                    } 
                    else if (movement == "UP")
                    {
                        priceChange = 0.1;
                    }

                    TransactionRecords transactionRec = new TransactionRecords
                    {
                        Symbol = currency,
                        Transaction_Type = movement,
                        Qty = (decimal)(totQty * priceChange)
                    };

                    dbContext.Add(transactionRec);
                    dbContext.SaveChanges();

                } catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
                
            };

            _channel.BasicConsume(queueName, true, consumer);
        }

    }
}
