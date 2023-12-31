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
    config.UseMessageRetry(r =>
    {
      r.Handle<RabbitMqConnectionException>();
      r.Interval(5, TimeSpan.FromSeconds(10));
    });

    config.Host(builder.Configuration["RabbitMq:Host"], "/", host =>
    {
      host.Username(builder.Configuration.GetValue("RabbitMq:Username", "guest"));
      host.Password(builder.Configuration.GetValue("RabbitMq:Password", "guest"));
    });

    config.ReceiveEndpoint("search-auction-created", e =>
    {
      e.UseMessageRetry(r => r.Interval(5, 5));

      e.ConfigureConsumer<AuctionCreatedConsumer>(context);
    });

    // config.ReceiveEndpoint("search-auction-updated", e =>
    // {
    //   e.UseMessageRetry(r => r.Interval(5, 5));

    //   e.ConfigureConsumer<AuctionUpdatedConsumer>(context);
    // });

    // config.ReceiveEndpoint("search-auction-deleted", e =>
    // {
    //   e.UseMessageRetry(r => r.Interval(5, 5));

    //   e.ConfigureConsumer<AuctionDeletedConsumer>(context);
    // });

    config.ConfigureEndpoints(context);
  });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(async () =>
{
  await Policy.Handle<TimeoutException>()
  .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(5))
  .ExecuteAndCaptureAsync(async () => await DbHelper.InitDb(app));
});

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetPolicy()
  => HttpPolicyExtensions
  .HandleTransientHttpError()
  .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
  .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));