using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.IO;
using System.Net;
using System.Net.WebSockets;

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
            t = Task.Factory.StartNew(() => Start(ns), ct);
        }

        protected async void Start(string ns)
        {
            while (blKeepRunning)
            {
                await WebSocket(cts.Token, ns).ConfigureAwait(false);
            }
        }

        protected async Task WebSocket(CancellationToken stoppingToken, string ns)
        {
            do
            {
                using (var socket = new ClientWebSocket())
                    try
                    {
                        Log.Debug("Trying to connect to Ditto using: " + conf.username + " " + conf.ditto_uri.ToString());
                        socket.Options.Credentials = new NetworkCredential(conf.username, conf.password);
                        await socket.ConnectAsync(conf.ditto_uri, CancellationToken.None).ConfigureAwait(false);
                        await Send(socket, ns).ConfigureAwait(false);
                        await Receive(socket).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Something went wrong...");
                        Log.Debug(ex.ToString());
                        System.Threading.Thread.Sleep(2000);
                    }
                    finally
                    {
                        // Restarting socket
                        Log.Error("Trying to restart websocket...");
                    }
            } while (!stoppingToken.IsCancellationRequested);

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

                    Log.Information("End of message...");
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    ms.Seek(0, SeekOrigin.Begin);
                    String str;
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                        str = await reader.ReadToEndAsync().ConfigureAwait(false);

                    Log.Information("Checking for valid Ditto response...");
                    if (str.StartsWith("{\"topic\":", StringComparison.Ordinal))
                    {
                        int start = str.IndexOf("thingId") + 10;
                        int end = str.IndexOf("policyId") - 3;
                        string thingId = str.Substring(start, end-start);
                        Log.Information("Status for " + thingId + " changed, informing .");;
                        Log.Debug(str);
                        SendToManager(str);
                    }
                }
            } while (true);
        }

        public abstract void SendToManager(string str);
        
        public void Stop()
        {
            blKeepRunning = false;
            cts.Cancel();
        }

    }
}
