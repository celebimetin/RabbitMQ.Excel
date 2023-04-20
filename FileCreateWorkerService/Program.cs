using FileCreateWorkerService;
using FileCreateWorkerService.Models;
using FileCreateWorkerService.Services;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        services.AddDbContext<AdventureWorks2019Context>(opt =>
        {
            opt.UseSqlServer(configuration.GetConnectionString("SqlServer"));
        });
        services.AddSingleton<RabbitMQClientService>();
        services.AddSingleton(sp => new ConnectionFactory() { Uri = new Uri(configuration.GetConnectionString("RabbitMQ")), DispatchConsumersAsync = true });

        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();