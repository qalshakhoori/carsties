using System.Net;
using MassTransit;
using Polly;
using Polly.Extensions.Http;
using SearchService;
using SearchService.Consumers;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<AuctionServiceHttpClient>()
  .AddPolicyHandler(GetPolicy());
builder.Services.AddMassTransit(m =>
{
  m.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

  m.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));

  m.UsingRabbitMq((context, config) =>
  {
    config.ReceiveEndpoint("search-auction-created", e =>
    {
      e.UseMessageRetry(r => r.Interval(5, 5));

      e.ConfigureConsumer<AuctionCreatedConsumer>(context);
    });

    config.ReceiveEndpoint("search-auction-updated", e =>
    {
      e.UseMessageRetry(r => r.Interval(5, 5));

      e.ConfigureConsumer<AuctionUpdatedConsumer>(context);
    });

    config.ReceiveEndpoint("search-auction-deleted", e =>
    {
      e.UseMessageRetry(r => r.Interval(5, 5));

      e.ConfigureConsumer<AuctionDeletedConsumer>(context);
    });

    config.ConfigureEndpoints(context);
  });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(async () =>
{
  try
  {
    await DbHelper.InitDb(app);
  }
  catch (Exception e)
  {
    Console.WriteLine(e);
  }
});

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetPolicy()
  => HttpPolicyExtensions
  .HandleTransientHttpError()
  .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
  .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));