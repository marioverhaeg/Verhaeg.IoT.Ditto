using System;
using Verhaeg.IoT.Processor;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;

namespace Verhaeg.IoT.Ditto
{
    public class DittoManager : QueueManager
    {
        // SingleTon
        private static DittoManager _instance = null;
        private static readonly object padlock = new object();
        private Configuration.Configuration_HTTP configuration;

        // Communication
        private HttpClient hc;
        private DittoClient dc;

        private DittoManager(string name) : base(name)
        {
            // Initiate Configuration
            configuration = Configuration.Configuration_HTTP.Instance("Configuration" + System.IO.Path.AltDirectorySeparatorChar + "Ditto_HTTP.json");

            hc = new HttpClient();
            hc.BaseAddress = configuration.URI();
            var byteArray = System.Text.Encoding.ASCII.GetBytes(configuration.Username() + ":" + configuration.Password());
            hc.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            dc = new DittoClient(hc);
        }

        public static DittoManager Instance()
        {
            lock (padlock)
            {
                if (_instance == null)
                {
                    _instance = new DittoManager("DittoManager");
                }
                return (DittoManager)_instance;
            }
        }

        public async Task<Thing> GetThing(string thingId)
        {
            Thing t = await dc.Things2Async(thingId);
            return t;
        }

        public NewThing ConvertThing(Thing t)
        {
            NewThing nt = new NewThing();
            nt.AdditionalProperties = t.AdditionalProperties;
            nt.Attributes = t.Attributes;
            nt.Definition = t.Definition;
            nt.Features = t.Features;
            nt.PolicyId = t.PolicyId;
            return nt;
        }

        protected override void Dispose()
        {
            _instance = null;
        }

        public DittoWebSocketResponse Parse(string str)
        {
            Log.Information("Trying to parse Ditto JSON response into Thing...");
            DittoWebSocketResponse dws = null;
            try
            {
                dws = JsonConvert.DeserializeObject<DittoWebSocketResponse>(str);
                Log.Information("Parsing JSON from Ditto into thing succeeded.");
            }
            catch (Exception ex)
            {
                Log.Error("Parsing JSON into thing failed.");
                Log.Debug(ex.ToString());
            }
            return dws;
        }


        protected override void Process(object obj)
        {
            if (obj != null)
            {
                Log.Information("Processing message...");
                NewThing t = (NewThing)obj;

                var att = t.Attributes.AdditionalProperties.Where(a => a.Key == "name").FirstOrDefault();
                try
                {
                    Log.Information("Trying to update thing with thingId: " + att.Value.ToString() + "...");
                    Task<Thing> tt = dc.Things3Async(att.Value.ToString(), null, null, t);
                    tt.Wait();
                    Log.Information("Thing with thingId: " + att.Value.ToString() + " updated.");
                }
                catch (Exception ex)
                {
                    Log.Error("Couldn't modify thing with thingId " + att.Value.ToString() + ", dropping update.");
                    Log.Fatal(ex.ToString());
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(obj));
            }
        }
    }
}
