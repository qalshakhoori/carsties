using BiddingService;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MongoDB.Driver;
using MongoDB.Entities;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddMassTransit(m =>
{
    m.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

    m.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("bids", false));

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
        config.ConfigureEndpoints(context);
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      options.Authority = builder.Configuration["IdentityServiceUrl"];
      options.RequireHttpsMetadata = false;
      options.TokenValidationParameters.ValidateAudience = false;
      options.TokenValidationParameters.NameClaimType = "username";
  });

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHostedService<CheckAuctionFinished>();
builder.Services.AddScoped<GrpcAuctionClient>();
var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

await Policy.Handle<TimeoutException>()
.WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(5))
.ExecuteAndCaptureAsync(async () =>
{
    await DB.InitAsync("BidDb", MongoClientSettings
    .FromConnectionString(builder.Configuration.GetConnectionString("BidDbConnection")));
});

app.Run();
