using AuctionService.Data;
using AuctionService.Entities;
using MassTransit;
using MassTransit.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WebMotions.Fake.Authentication.JwtBearer;

namespace AuctionService.IntegrationTests;

public class CustomWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
  private PostgreSqlContainer _postgresSqlContainer = new PostgreSqlBuilder().Build();

  public async Task InitializeAsync()
  {
    await _postgresSqlContainer.StartAsync();
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureTestServices(services =>
    {
      services.RemoveDBContext<AuctionDbContext>();

      services.AddDbContext<AuctionDbContext>(options =>
      {
        options.UseNpgsql(_postgresSqlContainer.GetConnectionString());
      });

      services.AddMassTransitTestHarness();

      services.EnsureCreated<AuctionDbContext>();

      services.AddAuthentication(FakeJwtBearerDefaults.AuthenticationScheme)
      .AddFakeJwtBearer(opt =>
      {
        opt.BearerValueType = FakeJwtBearerBearerValueType.Jwt;
      });
    });

    base.ConfigureWebHost(builder);
  }

  Task IAsyncLifetime.DisposeAsync() => _postgresSqlContainer.DisposeAsync().AsTask();
}
