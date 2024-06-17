using System;
using Verhaeg.IoT.Processor;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Verhaeg.IoT.Ditto.Api20;

namespace Verhaeg.IoT.Ditto
{
    public class DittoManager : QueueManager
    {
        // SingleTon
        private static DittoManager _instance = null;
        private static readonly object padlock = new object();
        private Configuration.Connection conf;

        // Communication
        private HttpClient hc;
        private DittoClient dc;

        private DittoManager(string name, string uri, string username, string password) : base(name)
        {
            // Initiate Configuration
            conf = new Configuration.Connection(uri, username, password);

            Log.Information("Starting DittoManager...");

            hc = new HttpClient();
            hc.BaseAddress = conf.ditto_uri;
            var byteArray = System.Text.Encoding.ASCII.GetBytes(conf.username + ":" + conf.password);
            hc.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            dc = new DittoClient(hc);

            Log.Information("DittoManager started.");
        }

        public static void Start(string uri, string username, string password)
        {
            lock (padlock)
            {
                if (_instance == null)
                {
                    _instance = new DittoManager("DittoManager", uri, username, password);
                }
            }
        }

        public static DittoManager Instance()
        {
            lock (padlock)
            {
                if (_instance == null)
                {
                    return null;
                }
                else
                {
                    return (DittoManager)_instance;
                }
            }
        }

        public Thing GetThing(string thingId)
        {
            try
            {
                Task<Thing> t = dc.Things2Async(thingId, "thingId,policyId,definition,attributes,features,_modified");
                t.Wait();
                return t.Result;
            }
            catch (Exception ex)
            {
                Log.Error("Could not find thing with thingId " + thingId);
                Log.Error(ex.ToString());
                return null;
            }
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

        


        protected override void Process(object obj)
        {
            if (obj != null)
            {
                NewThing t = (NewThing)obj;
                var att = t.Attributes.AdditionalProperties.Where(a => a.Key == "name").FirstOrDefault();

                //t.PolicyId = att.Value.ToString();
                try
                {
                    Log.Debug("Trying to update thing with thingId: " + att.Value.ToString());
                    Task<Thing> tt = dc.Things3Async(att.Value.ToString(), null, null, t);
                    tt.Wait();
                    Log.Debug("Thing with thingId: " + att.Value.ToString() + " updated.");
                }
                catch (Exception ex)
                {
                    Log.Error("Couldn't modify thing with thingId " + att.Value.ToString());
                    Log.Error(ex.ToString());
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(obj));
            }
        }
    }
}
