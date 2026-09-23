using GreenhouseApp.Models;

namespace GreenhouseApp.Messages;

public record OpenAddOrEditDeviceMessage(Device? Device = null);