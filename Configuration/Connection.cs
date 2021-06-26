using System.Runtime.CompilerServices;

namespace Verhaeg.IoT.Ditto.Configuration
{
    public class Connection
    {
        public System.Uri ditto_uri { get; private set; }
        public string username { get; private set; }
        public string password { get; private set; }

        public Connection(string uri, string username, string password)
        {
            this.ditto_uri = new System.Uri(uri);
            this.username = username;
            this.password = password;
        }
    }
}
