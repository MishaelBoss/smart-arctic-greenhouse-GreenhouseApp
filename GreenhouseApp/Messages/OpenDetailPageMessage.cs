namespace GreenhouseApp.Messages;

public record OpenDetailPageMessage(int DeviceId, string DeviceName, string ConnectionType = "wifi");