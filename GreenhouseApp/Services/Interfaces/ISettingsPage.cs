namespace GreenhouseApp.Services.Interfaces;

internal interface ISettingsPage
{
    bool HasChanges { get; }
    void Save();
}
