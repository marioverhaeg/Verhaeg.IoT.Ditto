using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.IO;
using System.Net;
using System.Net.WebSockets;

using Newtonsoft.Json;

namespace Verhaeg.IoT.Ditto
{
    public abstract class DittoWebSocketManager
    {
        // Store configuration
        private Configuration.Connection conf;

        // Fields
        protected Boolean blKeepRunning;
        protected EventWaitHandle ewh;
        protected Task t;
        protected CancellationTokenSource cts;
        protected ClientWebSocket cw;

        // Logging
        protected Serilog.ILogger Log;

        protected DittoWebSocketManager(string ns, string type, string uri, string username, string password)
        {
            // Serilog Configuration
            Log = Processor.Log.CreateLog(type);

            blKeepRunning = true;
            ewh = new AutoResetEvent(false);
            // Start new read thread
            cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            // Initiate Configuration
            conf = new Configuration.Connection(uri, username, password);
            Log.Debug("Starting new task to connect to Ditto Websocket...");
            Log.Debug("Using Ditto namespace: " + ns);
            t = Task.Factory.StartNew(() => Start(ns), ct);
        }

        public async void Start(string ns)
        {
            while (blKeepRunning)
            {
                Log.Information("Websocket started.");
                await WebSocket(cts.Token, ns).ConfigureAwait(false);
                Log.Error("Restarting websocket...");
            }
            Log.Debug("Stopping DittoWebSocketManager...");
        }

        protected async Task WebSocket(CancellationToken stoppingToken, string ns)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                cw = new ClientWebSocket();
                try
                {
                    Log.Debug("Trying to connect to Ditto using: " + conf.username + " " + conf.ditto_uri.ToString());
                    cw.Options.Credentials = new NetworkCredential(conf.username, conf.password);
                    await cw.ConnectAsync(conf.ditto_uri, stoppingToken).ConfigureAwait(false);
                    Log.Debug("Connected to Ditto.");
                    await Send(cw, ns, stoppingToken).ConfigureAwait(false);
                    await Receive(cw, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error("Websocket aborted.");
                    Log.Debug(ex.ToString());
                    Thread.Sleep(10000);
                }
            }
        }

        private async Task Send(ClientWebSocket socket, string data, CancellationToken stoppingToken)
        {
            if (socket.State == WebSocketState.Open)
            {
                ArraySegment<byte> asb = Encoding.UTF8.GetBytes(data);
                await socket.SendAsync(asb, WebSocketMessageType.Text, true, stoppingToken).ConfigureAwait(false);
            }
            else
            {
                Log.Error("ClientWebSocket closed, cannot send message: " + data);
            }
        }

        private async Task Receive(ClientWebSocket socket, CancellationToken stoppingToken)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            do
            {
                WebSocketReceiveResult result;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, stoppingToken).ConfigureAwait(false);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);

                    Log.Debug("End of message...");
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    ms.Seek(0, SeekOrigin.Begin);
                    String str;
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                        str = await reader.ReadToEndAsync().ConfigureAwait(false);

                    Log.Debug("Checking for valid Ditto response...");
                    if (str.StartsWith("{\"topic\":", StringComparison.Ordinal))
                    {
                        int start = str.IndexOf("thingId") + 10;
                        int end = str.IndexOf("policyId") - 3;
                        string thingId = str.Substring(start, end-start);
                        Log.Debug("Status for " + thingId + " changed, informing Manager.");
                        Log.Debug(str);
                        SendToManager(str);
                    }
                }
            } while (stoppingToken.IsCancellationRequested == false);
        }

        public void SendToManager(string str)
        {
            Log.Debug("Trying to parse Ditto JSON response into Ditto Thing...");
            DittoWebSocketResponse dws = Parse(str);

            if (dws != null)
            {
                Log.Debug("Received update from " + dws.value.ThingId);
                Log.Debug("JSON parsed to Ditto Thing, extracting values...");
                Extract(dws);
            }
            else
            {
                Log.Error("Could not parse response from Ditto.");
            }
        }

        protected abstract void Extract(DittoWebSocketResponse dws);

        public DittoWebSocketResponse Parse(string str)
        {
            DittoWebSocketResponse dws = null;
            try
            {
                dws = JsonConvert.DeserializeObject<DittoWebSocketResponse>(str);
            }
            catch (Exception ex)
            {
                Log.Error("Parsing JSON into thing failed.");
                Log.Debug(ex.ToString());
            }
            return dws;
        }

        public bool IsRunning()
        {
            if (cw != null)
            {
                if (cw.State == WebSocketState.Open)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void Stop()
        {
            blKeepRunning = false;
            cts.Cancel();
            if (cw != null)
            {
                while (cw.State == WebSocketState.Open)
                {
                    Log.Debug("WebSocketState.Open, trying to close.");
                    try
                    {
                        cw.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);

                        while (cw.State != WebSocketState.Closed)
                        {
                            Log.Debug("Waiting for websocket to be closed...");
                            Thread.Sleep(5000);
                        }

                        Log.Debug("WebSocket closed.");
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Could not close websocket, retrying in 10 seconds...");
                        Log.Debug(ex.ToString());
                        Thread.Sleep(10000);
                    }
                }
            }
            else
            {
                Log.Debug("WebSocket not running.");
            }
        }

    }
}
