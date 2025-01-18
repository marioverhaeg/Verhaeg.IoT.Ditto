using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;
using Verhaeg.IoT.Ditto.Api20;

namespace Verhaeg.IoT.Ditto
{

    public class DittoWebSocketResponse
    {
        public string topic { get; set; }
        public Headers headers { get; set; }
        public string path { get; set; }
        public Value value { get; set; }
        public int revision { get; set; }
        public DateTime timestamp { get; set; }        

        public DittoWebSocketResponse(string thingId)
        {
            this.timestamp = DateTime.Now;
            this.value = new Value();
            this.value.ThingId = thingId;
        }
    }

    public partial class Headers
    {
        public string correlationid { get; set; }
        public string xforwardedfor { get; set; }
        public int version { get; set; }
        public string accept { get; set; }
        public object[] requestedacks { get; set; }
        public string authorization { get; set; }
        public string xrealip { get; set; }
        public string xforwardeduser { get; set; }
        public string xdittodummyauth { get; set; }
        public bool responserequired { get; set; }
        public string dittooriginator { get; set; }
        public string host { get; set; }
        public string contenttype { get; set; }
    }

    public class Value : Thing
    {
       
    }
}
