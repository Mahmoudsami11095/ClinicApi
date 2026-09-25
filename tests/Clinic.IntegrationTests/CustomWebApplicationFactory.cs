using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    static CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        Environment.SetEnvironmentVariable("Jwt__Key", "SuperSecretTestingKeyThatIsAtLeast32BytesLong!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "ClinicApiTesting");
        Environment.SetEnvironmentVariable("Jwt__Audience", "ClinicAppTesting");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<ClinicDbContext>(options =>
            {
                options.UseInMemoryDatabase("ClinicIntegrationTestDb_" + Guid.NewGuid().ToString());
            });
        });

        builder.UseEnvironment("Testing");
    }
}
