using MassTransit;
using NotificationService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(m =>
{
  m.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

  m.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("nt", false));

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

builder.Services.AddSignalR();

var app = builder.Build();

app.MapHub<NotificationHub>("/notifications");

app.Run();
