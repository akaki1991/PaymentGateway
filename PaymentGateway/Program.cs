using PaymentGateway;
using PaymentGateway.Services.Factories;
using PaymentGateway.Services.Implementations;
using PaymentGateway.Services.Interfaces;
using Serilog;

var exePath = Environment.ProcessPath;
var directory = Path.GetDirectoryName(exePath);
Directory.SetCurrentDirectory(directory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(@"C:\PaymentGateWayLogs\payment-gateway-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Payment Gateway";
});

builder.Services.AddTransient<ITransactionDataParser, TransactionDataParser>();
builder.Services.AddSingleton<TCPServer>();
builder.Services.AddSingleton<IMessageStrategyFactory, MessageStrategyFactory>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();
