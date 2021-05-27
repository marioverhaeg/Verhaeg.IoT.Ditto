using System.Runtime.CompilerServices;


namespace Verhaeg.IoT.Ditto
{
    public class Configuration_WS : Configuration.Device
    {

        // SingleTon
        private static Configuration_WS _instance = null;

        public static Configuration_WS Instance(string path, [CallerMemberName] string caller = "")
        {
            lock (padlock)
            {
                if (_instance == null)
                {
                    _instance = new Configuration_WS(path);
                }
                return (Configuration_WS)_instance;
            }
        }


        private Configuration_WS(string path) : base (path, "Ditto_WS")
        {
            
        }

       
    }
}
