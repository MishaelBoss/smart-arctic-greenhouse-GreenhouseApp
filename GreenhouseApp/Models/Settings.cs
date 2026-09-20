namespace GreenhouseApp.Models;

public class Settings
{
    public string ServerUrl { get; set; } = "http://localhost:8000";
    public int DeviceId { get; set; } = 1;

    public int Soil1RawDry { get; set; } = 4095;
    public int Soil1RawWet { get; set; } = 755;
    public int Soil2RawDry { get; set; } = 4095;
    public int Soil2RawWet { get; set; } = 755;

    public double PumpOnBelow { get; set; } = 30.0;
    public double PumpOffAbove { get; set; } = 70.0;
    public double RoofOpenAbove { get; set; } = 28.0;
    public double RoofCloseBelow { get; set; } = 22.0;
}