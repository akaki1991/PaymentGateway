using PaymentGateway;
using PaymentGateway.Services.Implementations;
using PaymentGateway.Services.Interfaces;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Payment Gateway";
});


builder.Services.AddTransient<ITransactionDataParser, TransactionDataParser>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
