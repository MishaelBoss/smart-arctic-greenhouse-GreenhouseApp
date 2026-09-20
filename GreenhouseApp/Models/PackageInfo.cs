using GreenhouseApp.Services.Interfaces;

namespace GreenhouseApp.Models;

public class PackageInfo (string name, string version) : IPackageInfo
{
    public string PackageName { get; set; } = name;
    public string PackageVersion { get; set; } = version;
}
