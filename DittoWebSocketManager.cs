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
            Log.Information("Starting new task to connect to Ditto Websocket...");
            t = Task.Factory.StartNew(() => Start(ns), ct);
        }

        public async void Start(string ns)
        {
            while (blKeepRunning)
            {
                await WebSocket(cts.Token, ns).ConfigureAwait(false);
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
                    await Send(cw, ns).ConfigureAwait(false);
                    await Receive(cw).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error("Websocket aborted.");
                    Log.Debug(ex.ToString());
                    System.Threading.Thread.Sleep(2000);
                }
            }
        }

        private async Task Send(ClientWebSocket socket, string data)
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task Receive(ClientWebSocket socket)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            do
            {
                WebSocketReceiveResult result;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
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
            } while (true);
        }

        public abstract void SendToManager(string str);

        public DittoWebSocketResponse Parse(string str)
        {
            Log.Debug("Trying to parse Ditto JSON response into Thing...");
            DittoWebSocketResponse dws = null;
            try
            {
                dws = JsonConvert.DeserializeObject<DittoWebSocketResponse>(str);
                Log.Debug("Parsing JSON from Ditto into thing succeeded.");
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
                if (cw.State == WebSocketState.Open)
                {
                    Log.Debug("WebSocketState.Open, trying to abort and close.");
                    try
                    {
                        cw.Abort();
                        Log.Debug("WebSocket closed.");
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Could not close websocket.");
                        Log.Debug(ex.ToString());
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
