namespace LaundryPOS.Infrastructure.IoT;

/// <summary>
/// Bound from configuration section "IoT:Mqtt" (see appsettings.json /
/// docker-compose.yml IoT__Mqtt__* environment variables).
/// </summary>
public class MqttOptions
{
    public string BrokerHost { get; set; } = "localhost";
    public int BrokerPort { get; set; } = 1883;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string BaseTopic { get; set; } = "laundrypos";
}
