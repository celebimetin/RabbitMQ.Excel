using ClosedXML.Excel;
using FileCreateWorkerService.Models;
using FileCreateWorkerService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;
using System.Data;
using System.Text;
using System.Text.Json;

namespace FileCreateWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly RabbitMQClientService _rabbitmqClientService;
        private readonly IServiceProvider _serviceProvider;
        private IModel _channel;
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, RabbitMQClientService rabbitmqClientService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _rabbitmqClientService = rabbitmqClientService;
            _serviceProvider = serviceProvider;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _channel = _rabbitmqClientService.Connect();
            _channel.BasicQos(0, 1, false);

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            _channel.BasicConsume(RabbitMQClientService.QueueName, false, consumer);
            consumer.Received += Cunsomer_Received;

            return Task.CompletedTask;
        }

        private async Task Cunsomer_Received(object sender, BasicDeliverEventArgs @event)
        {
            await Task.Delay(5000);

            var createExcelMessage = JsonSerializer.Deserialize<CreateExcelMessage>(Encoding.UTF8.GetString(@event.Body.ToArray()));

            using var ms = new MemoryStream();

            var wd = new XLWorkbook();
            var ds = new DataSet();

            ds.Tables.Add(GetData("products"));
            wd.Worksheets.Add(ds);
            wd.SaveAs(ms);

            MultipartFormDataContent multipartFormDataContent = new();
            multipartFormDataContent.Add(new ByteArrayContent(ms.ToArray()), "file", Guid.NewGuid().ToString() + ".xlsx");

            var baseUrl = "https://localhost:7204/api/files";
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync($"{baseUrl}?fileId={createExcelMessage.FileId}", multipartFormDataContent);

                if (response.IsSuccessStatusCode)
                {
                    _channel.BasicAck(@event.DeliveryTag, false);
                    _logger.LogInformation($"File (Id: {createExcelMessage.FileId}) was created");
                }
            }
        }

        private DataTable GetData(string tableName)
        {
            List<Product> products;

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AdventureWorks2019Context>();
                products = context.Products.ToList();
            }

            var dataTable = new DataTable { TableName = tableName };
            dataTable.Columns.Add("ProductId", typeof(int));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("ProductNumber", typeof(string));
            dataTable.Columns.Add("Color", typeof(string));

            products.ForEach(x => dataTable.Rows.Add(
                x.ProductId,
                x.Name,
                x.ProductNumber,
                x.Color
                ));

            return dataTable;
        }
    }
}