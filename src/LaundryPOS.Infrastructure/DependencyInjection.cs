using LaundryPOS.Domain.Interfaces.Repositories;
using LaundryPOS.Domain.Interfaces.Services;
using LaundryPOS.Infrastructure.IoT;
using LaundryPOS.Infrastructure.Payments;
using LaundryPOS.Infrastructure.Persistence;
using LaundryPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LaundryPOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<LaundryDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(LaundryDbContext).Assembly.FullName)
            ));

        // Unit of Work & Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Auth Services
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Products
        services.AddScoped<IProductExcelService, ProductExcelService>();

        // Payment Gateways
        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();
        services.AddScoped<StripeGateway>();
        services.AddScoped<MercadoPagoGateway>();
        services.AddScoped<OpenPayGateway>();
        services.AddScoped<ClipGateway>();
        services.AddScoped<BBVAGateway>();
        services.AddScoped<CashGateway>();

        // IoT Drivers
        services.AddScoped<IIoTDriverFactory, IoTDriverFactory>();
        services.AddScoped<Esp32Driver>();
        services.AddScoped<PlcDriver>();
        services.AddScoped<RaspberryPiDriver>();
        services.AddScoped<WascomatDriver>();
        services.AddHttpClient("WascomatRelay");

        // MQTT (real-time comms with ESP32 machine controllers)
        services.Configure<MqttOptions>(configuration.GetSection("IoT:Mqtt"));
        services.AddSingleton<MqttConnectionManager>();
        services.AddSingleton<IMqttPublisherService>(sp => sp.GetRequiredService<MqttConnectionManager>());
        services.AddHostedService(sp => sp.GetRequiredService<MqttConnectionManager>());

        return services;
    }
}
