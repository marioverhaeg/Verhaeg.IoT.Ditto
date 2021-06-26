# Introduction
[Eclipse Ditto™](https://www.eclipse.org/ditto/) is an open source framework for creating and managing digital twins in the IoT. Verhaeg.IoT.Ditto is a small class library that wraps the Ditto websocket and REST interface and makes this usable in other projects. You can find more information on https://www.marioverhaeg.nl/tag/ditto/

# Usage

## DittoManager
DittoManager.Write(obj) accepts objects of the type "NewThing", part of the Ditto-API-2.cs file. The object is taken from the queue and updated to Ditto. The DittoManager is a singleton class that you need to start with the correct parameters: the Ditto URI, username, and password.

## DittoWebSockerManager
The DittoWebSocketManager connects to the Ditto websocket and subscribes to the events based on the ns parameter. The formatting of the ns parameter is described on the Ditto website: https://www.eclipse.org/ditto/httpapi-protocol-bindings-websocket.html

The example below will subscribe to all events:
```
START-SEND-EVENTS
```
The example below will subscribe to selected events of a specific thingId:
```
START-SEND-EVENTS?filter=like(thingId,\"Verhaeg.IoT.Host:Chromecast\")
```

