using System;
using System.Text.Json.Serialization;

namespace GreenhouseApp.Models;

public class Device
{
    [JsonPropertyName("id")]           public int Id { get; set; }
    [JsonPropertyName("name")]         public string Name { get; set; } = "";
    [JsonPropertyName("api_key")]      public string ApiKey { get; set; } = "";
    [JsonPropertyName("connection_type")] public string ConnectionType { get; set; } = "wifi";
    [JsonPropertyName("last_seen")]    public DateTime? LastSeen { get; set; }
}

public class Telemetry
{
    [JsonPropertyName("soil1_raw")]      public int Soil1Raw { get; set; }
    [JsonPropertyName("soil1_moisture")] public double Soil1Moisture { get; set; }
    [JsonPropertyName("soil2_raw")]      public int Soil2Raw { get; set; }
    [JsonPropertyName("soil2_moisture")] public double Soil2Moisture { get; set; }
    [JsonPropertyName("temperature")]    public double? Temperature { get; set; }
    [JsonPropertyName("humidity")]       public double? Humidity { get; set; }
    [JsonPropertyName("timestamp")]      public DateTime Timestamp { get; set; }
    [JsonPropertyName("is_fresh")]       public bool IsFresh { get; set; } = true; 
    [JsonPropertyName("age_seconds")]    public double AgeSeconds { get; set; }
}