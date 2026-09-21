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
    // ==== Датчики почвы ====
    [JsonPropertyName("soil1_raw")]      public int    Sensor1Raw      { get; set; }
    [JsonPropertyName("soil1_moisture")] public double Sensor1Moisture { get; set; }
    [JsonPropertyName("soil2_raw")]      public int    Sensor2Raw      { get; set; }
    [JsonPropertyName("soil2_moisture")] public double Sensor2Moisture { get; set; }

    // ==== DHT #1 — датчик 1 (внутренний) ====
    [JsonPropertyName("temperature")]    public double? Temperature1 { get; set; }   // ← "temperature" без 1
    [JsonPropertyName("humidity")]       public double? Humidity1    { get; set; }   // ← "humidity" без 1

    // ==== DHT #2 — датчик 2 (наружный) ====
    [JsonPropertyName("temperature2")]   public double? Temperature2 { get; set; }
    [JsonPropertyName("humidity2")]      public double? Humidity2    { get; set; }

    // ==== Освещённость ====
    [JsonPropertyName("light1")]         public int? Light1 { get; set; }
    [JsonPropertyName("light2")]         public int? Light2 { get; set; }

    // ==== Состояние исполнительных механизмов ====
    [JsonPropertyName("pump")]           public bool? Pump  { get; set; }
    [JsonPropertyName("light")]          public bool? Light { get; set; }
    [JsonPropertyName("roof")]           public bool? Roof  { get; set; }

    // ==== События от ESP32 (двигатели/реле) ====
    [JsonPropertyName("ev")]             public string[]? Events { get; set; }

    // ==== Служебные ====
    [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }

    [JsonIgnore] public bool   IsFresh    { get; set; } = true;
    [JsonIgnore] public double AgeSeconds { get; set; }
}