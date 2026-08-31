using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LaundryPOS.Infrastructure.Payments;

public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _gateways = new(StringComparer.OrdinalIgnoreCase);

    public PaymentGatewayFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // Register available gateways
        _gateways["Stripe"] = typeof(StripeGateway);
        _gateways["MercadoPago"] = typeof(MercadoPagoGateway);
        _gateways["OpenPay"] = typeof(OpenPayGateway);
        _gateways["Clip"] = typeof(ClipGateway);
        _gateways["BBVA"] = typeof(BBVAGateway);
        _gateways["Cash"] = typeof(CashGateway);
    }

    public IPaymentGateway GetGateway(string gatewayName)
    {
        if (!_gateways.TryGetValue(gatewayName, out var type))
            throw new ArgumentException($"Payment gateway '{gatewayName}' is not registered.");

        return (IPaymentGateway)(_serviceProvider.GetService(type)
            ?? throw new InvalidOperationException($"Gateway {gatewayName} is not configured in DI."));
    }

    public IReadOnlyList<string> GetAvailableGateways() => _gateways.Keys.ToList().AsReadOnly();
}

// ─── Gateway implementations (stubs ready for real integration) ───

public class StripeGateway : IPaymentGateway
{
    private readonly ILogger<StripeGateway> _logger;
    public string GatewayName => "Stripe";

    public StripeGateway(ILogger<StripeGateway> logger) { _logger = logger; }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing Stripe payment for {Amount} {Currency}", request.Amount, request.Currency);
        // TODO: Implement real Stripe API integration
        await Task.Delay(100, ct); // Simulate API call
        return new PaymentResult
        {
            Success = true,
            AuthorizationNumber = $"STRIPE-{Guid.NewGuid():N}"[..20],
            GatewayTransactionId = $"pi_{Guid.NewGuid():N}",
            Status = PaymentStatus.Authorized
        };
    }

    public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing Stripe refund for {TransactionId}", transactionId);
        await Task.Delay(100, ct);
        return new PaymentResult { Success = true, Status = PaymentStatus.Refunded };
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        return new PaymentStatusResult { GatewayTransactionId = transactionId, Status = PaymentStatus.Completed };
    }
}

public class MercadoPagoGateway : IPaymentGateway
{
    private readonly ILogger<MercadoPagoGateway> _logger;
    public string GatewayName => "MercadoPago";

    public MercadoPagoGateway(ILogger<MercadoPagoGateway> logger) { _logger = logger; }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing MercadoPago payment for {Amount} {Currency}", request.Amount, request.Currency);
        await Task.Delay(100, ct);
        return new PaymentResult
        {
            Success = true,
            AuthorizationNumber = $"MP-{Guid.NewGuid():N}"[..20],
            GatewayTransactionId = $"mp_{Guid.NewGuid():N}",
            Status = PaymentStatus.Authorized
        };
    }

    public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return new PaymentResult { Success = true, Status = PaymentStatus.Refunded };
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        return new PaymentStatusResult { GatewayTransactionId = transactionId, Status = PaymentStatus.Completed };
    }
}

public class OpenPayGateway : IPaymentGateway
{
    public string GatewayName => "OpenPay";

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return new PaymentResult
        {
            Success = true,
            AuthorizationNumber = $"OP-{Guid.NewGuid():N}"[..20],
            GatewayTransactionId = $"op_{Guid.NewGuid():N}",
            Status = PaymentStatus.Authorized
        };
    }

    public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return new PaymentResult { Success = true, Status = PaymentStatus.Refunded };
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        return new PaymentStatusResult { GatewayTransactionId = transactionId, Status = PaymentStatus.Completed };
    }
}

public class ClipGateway : IPaymentGateway
{
    public string GatewayName => "Clip";

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return new PaymentResult
        {
            Success = true,
            AuthorizationNumber = $"CLIP-{Guid.NewGuid():N}"[..20],
            GatewayTransactionId = $"clip_{Guid.NewGuid():N}",
            Status = PaymentStatus.Authorized
        };
    }

    public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return new PaymentResult { Success = true, Status = PaymentStatus.Refunded };
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        return new PaymentStatusResult { GatewayTransactionId = transactionId, Status = PaymentStatus.Completed };
    }
}

public class BBVAGateway : IPaymentGateway
{
    public string GatewayName => "BBVA";

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return new PaymentResult
        {
            Success = true,
            AuthorizationNumber = $"BBVA-{Guid.NewGuid():N}"[..20],
            GatewayTransactionId = $"bbva_{Guid.NewGuid():N}",
            Status = PaymentStatus.Authorized
        };
    }

    public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return new PaymentResult { Success = true, Status = PaymentStatus.Refunded };
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        return new PaymentStatusResult { GatewayTransactionId = transactionId, Status = PaymentStatus.Completed };
    }
}

public class CashGateway : IPaymentGateway
{
    public string GatewayName => "Cash";

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(new PaymentResult
        {
            Success = true,
            AuthorizationNumber = $"CASH-{DateTime.UtcNow:yyyyMMddHHmmss}",
            GatewayTransactionId = $"cash_{Guid.NewGuid():N}",
            Status = PaymentStatus.Completed
        });
    }

    public Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, CancellationToken ct = default)
    {
        return Task.FromResult(new PaymentResult { Success = true, Status = PaymentStatus.Refunded });
    }

    public Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default)
    {
        return Task.FromResult(new PaymentStatusResult { GatewayTransactionId = transactionId, Status = PaymentStatus.Completed });
    }
}
