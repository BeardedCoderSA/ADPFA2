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
            consumer.Received += (sender, evnt) =>
            {
                var body = evnt.Body.ToArray();
                Debug.WriteLine(body);
                try
                {
                    var cryptoData = JsonConvert.DeserializeObject<walletItems>(Encoding.UTF8.GetString(body));

                    TransactionRecords transaction = new TransactionRecords
                    {
                        Symbol = cryptoData.Symbol,
                        Qty = (decimal)cryptoData.Qty
                    };

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetService<WalletDbContext>();

                    dbContext.Add(transaction);
                    dbContext.SaveChanges();
                } catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                
            };

            _channel.BasicConsume(queueName, true, consumer);
        }

    }
}
