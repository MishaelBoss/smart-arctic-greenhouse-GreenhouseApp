using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GreenhouseApp.Models;

namespace GreenhouseApp.Services;

public class ApiClient(string baseUrl = "http://localhost:8000")
{
    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromSeconds(5)
    };

    public void SetBaseUrl(string url)
    {
        _http.BaseAddress = new Uri(url);
    }
    
    public async Task<List<Device>> GetDevicesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<Device>>("/api/devices") ?? [];
        }
        catch
        {
            return [];
        }
    }
    
    public async Task<Telemetry?> GetLatestAsync(int deviceId)
    {
        try
        {
            return await _http.GetFromJsonAsync<Telemetry>(
                $"/api/devices/{deviceId}/latest");
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<List<Telemetry>> GetHistoryAsync(int deviceId, int limit = 100)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<Telemetry>>(
                $"/api/devices/{deviceId}/history?limit={limit}") ?? [];
        }
        catch
        {
            return [];
        }
    }
    
    public async Task<bool> SendCommandAsync(int deviceId, string action)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                $"/api/devices/{deviceId}/command",
                new { action });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}