# Verhaeg.IoT.Ditto

[![NuGet](https://img.shields.io/nuget/v/Verhaeg.IoT.Ditto)](https://www.nuget.org/packages/Verhaeg.IoT.Ditto/) [![GitHub](https://img.shields.io/badge/source-GitHub-blue)](https://github.com/marioverhaeg/Verhaeg.IoT.Ditto) [![Eclipse Ditto](https://img.shields.io/badge/Eclipse_Ditto-2.0+-blue)](https://www.eclipse.org/ditto/)

**Verhaeg.IoT.Ditto** is a .NET class library that wraps the Eclipse Ditto WebSocket and REST interfaces, providing a convenient API for integrating digital twin capabilities into your IoT applications.

Eclipse Ditto is an open-source framework for creating and managing digital twins in the IoT. It provides a scalable backend for IoT solutions with thing management, policies, and real-time event processing.

---

## Features

- REST API Client: Auto-generated NSwag clients for Ditto API v2.0 and v2.1
- WebSocket Support: Real-time event streaming with automatic reconnection
- Queue-Based Processing: Reliable queuing for thing updates via QueueManager
- Singleton Pattern: Easy singleton access to managers
- Type-Safe Models: Strongly-typed C# models for Ditto entities
- Authentication: Built-in basic authentication support
- Logging: Integrated Serilog logging

---

## Installation

### NuGet Package

Install-Package Verhaeg.IoT.Ditto

Or via .NET CLI:

dotnet add package Verhaeg.IoT.Ditto

### Prerequisites

- .NET 10.0 or later
- Access to an Eclipse Ditto instance (v2.0+)

---

## Quick Start

### Initialize DittoManager

```csharp
using Verhaeg.IoT.Ditto;

DittoManager.Start(
    uri: "https://your-ditto-instance.com",
    username: "ditto",
    password: "ditto"
);
var manager = DittoManager.Instance();
```

### Create and Update Things

```csharp
using Verhaeg.IoT.Ditto.Api20;

NewThing newThing = new NewThing
{
    Definition = "Verhaeg.IoT.Energy:forecast:1.0.0",
    Attributes = new Attributes
    {
        AdditionalProperties = new Dictionary<string, object>
        {
            { "name", "Verhaeg.IoT.Energy.Forecast:Device001" },
            { "location", "Amsterdam" },
            { "temperature", 22.5 }
        }
    }
};
DittoManager.Instance().Write(newThing);
```

### Retrieve Things

```csharp
Thing existingThing = DittoManager.Instance().GetThing("Verhaeg.IoT.Energy.Forecast:Device001");
if (existingThing != null)
{
    Console.WriteLine("Found thing: " + existingThing.ThingId);
}
```

---

## API Reference

### DittoManager

Singleton manager for REST API communication with Eclipse Ditto.

**Methods:**
- Start(uri, username, password): Initialize with connection credentials
- Instance(): Returns the singleton instance
- GetThing(thingId): Retrieves a specific Thing by its ID
- Write(NewThing): Queues a NewThing for creation/update in Ditto
- ConvertThing(Thing): Converts a Thing to a NewThing
- Dispose(): Cleans up resources

### DittoWebSocketManager

Abstract base class for WebSocket connections with real-time event streaming.

**Constructor:**
DittoWebSocketManager(string ns, string type, string uri, string username, string password)

**Parameters:**
- ns: Namespace filter for event subscription
- type: Logger type identifier
- uri: Ditto instance URI
- username: Authentication username
- password: Authentication password

**Namespace Filter Examples:**
- START-SEND-EVENTS: Subscribe to all events
- START-SEND-EVENTS?filter=like(thingId,"Verhaeg.IoT.Host:*"): Subscribe to events for specific namespace
- START-SEND-EVENTS?filter=eq(thingId,"my.thing:123"): Subscribe to events for a specific thing

**Methods:**
- Start(ns): Starts the WebSocket connection
- Stop(): Stops the WebSocket connection
- IsRunning(): Checks if WebSocket is connected
- Parse(string): Parses JSON to DittoWebSocketResponse
- Extract(DittoWebSocketResponse): Abstract - override to handle events
- SendToManager(string): Processes incoming messages

---

## Usage Examples

### Custom WebSocket Event Handler

```csharp
public class MyThingMonitor : DittoWebSocketManager
{
    public MyThingMonitor(string uri, string username, string password)
        : base("START-SEND-EVENTS?filter=like(thingId,\"my.namespace:*\")",
               "MyThingMonitor",
               uri,
               username,
               password)
    {
    }

    protected override void Extract(DittoWebSocketResponse dws)
    {
        string thingId = dws.value.ThingId;
        Console.WriteLine("Received update for " + thingId);
        Console.WriteLine("Timestamp: " + dws.timestamp);
        Console.WriteLine("Revision: " + dws.revision);
        ProcessThingUpdate(thingId, dws.value.Attributes);
    }

    private void ProcessThingUpdate(string thingId, Attributes attributes)
    {
        // Custom logic here
    }
}

// Usage
var monitor = new MyThingMonitor(
    "wss://your-ditto-instance.com",
    "ditto",
    "ditto"
);
```

### Temperature Monitor Example

```csharp
public class TemperatureMonitor : DittoWebSocketManager
{
    public event Action<string, double> TemperatureChanged;

    public TemperatureMonitor(string uri, string username, string password)
        : base("START-SEND-EVENTS?filter=like(thingId,\"sensor.temperature:*\")",
               "TemperatureMonitor",
               uri, username, password)
    {
    }

    protected override void Extract(DittoWebSocketResponse dws)
    {
        if (dws.value.Attributes?.AdditionalProperties != null
            && dws.value.Attributes.AdditionalProperties.TryGetValue("temperature", out var tempObj)
            && tempObj is double temperature)
        {
            TemperatureChanged?.Invoke(dws.value.ThingId, temperature);
        }
    }
}

// Usage
var tempMonitor = new TemperatureMonitor(uri, username, password);
tempMonitor.TemperatureChanged += (thingId, temp) => {
    Console.WriteLine(thingId + ": Temperature is " + temp + "C");
};
```

---

## Project Structure

```
Verhaeg.IoT.Ditto/
├── Configuration/
│   └── Connection.cs          # Connection configuration storage
├── Ditto-API-20.cs            # NSwag-generated API v2.0 client
├── Ditto-API-21.cs            # NSwag-generated API v2.1 client
├── DittoManager.cs            # REST API manager (singleton)
├── DittoWebSocket.cs          # WebSocket response models
├── DittoWebSocketManager.cs  # WebSocket manager base class
├── Verhaeg.IoT.Ditto.csproj   # Project configuration
└── README.md                  # This documentation
```

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Newtonsoft.Json | 13.0.4 | JSON serialization |
| Serilog | 4.4.0 | Logging framework |
| Serilog.Settings.Configuration | 10.0.1 | Serilog configuration |
| Serilog.Sinks.PeriodicBatching | 5.0.0 | Logging batching |
| Verhaeg.IoT.Processor | * | Queue management and base classes |

---

## Configuration

The library uses Configuration.Connection class to store connection settings:

```csharp
new Configuration.Connection(
    uri: "https://ditto.example.com",
    username: "user",
    password: "pass"
);
```

---

## Documentation

- [Eclipse Ditto Official Documentation](https://www.eclipse.org/ditto/documentation.html)
- [Ditto Protocol Specification](https://www.eclipse.org/ditto/protocol-specification-things.html)
- [Ditto HTTP API](https://www.eclipse.org/ditto/httpapi-protocol-bindings.html)
- [Ditto WebSocket Binding](https://www.eclipse.org/ditto/httpapi-protocol-bindings-websocket.html)

---

## More Information

For more information, examples, and updates, visit:
- [Mario Verhaeg Blog - Ditto Tag](https://www.marioverhaeg.nl/tag/ditto/)
- [GitHub Repository](https://github.com/marioverhaeg/Verhaeg.IoT.Ditto)
