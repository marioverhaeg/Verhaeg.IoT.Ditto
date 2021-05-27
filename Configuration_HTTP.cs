using System.Runtime.CompilerServices;
namespace Verhaeg.IoT.Configuration
{
    public class Configuration_HTTP : Configuration.Device
    {

        // SingleTon
        private static Configuration_HTTP _instance = null;

        public static Configuration_HTTP Instance(string path, [CallerMemberName] string caller = "")
        {
            lock (padlock)
            {
                if (_instance == null)
                {
                    _instance = new Configuration_HTTP(path);
                }
                return (Configuration_HTTP)_instance;
            }
        }


        private Configuration_HTTP(string path) : base (path, "Ditto_HTTP")
        {
            
        }

       
    }
}
