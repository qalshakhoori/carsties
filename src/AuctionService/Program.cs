using AuctionService;
using AuctionService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionDbContext>(
  opt =>
  {
    opt.UseNpgsql(connectionString);
  }
);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddMassTransit(m =>
{
  m.AddEntityFrameworkOutbox<AuctionDbContext>(o =>
  {
    o.QueryDelay = TimeSpan.FromSeconds(10);

    o.UsePostgres();
    o.UseBusOutbox();
  });

  m.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>();

  m.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));

  m.UsingRabbitMq((context, config) =>
  {
    config.ConfigureEndpoints(context);
  });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

try
{
  Seed.InitializeDB(app);
}
catch (Exception e)
{
  Console.WriteLine(e);
  throw;
}

app.Run();
