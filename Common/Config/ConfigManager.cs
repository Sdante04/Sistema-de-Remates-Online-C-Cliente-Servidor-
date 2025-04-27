using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Config
{
    public class ConfigManager
    {
        public string Readsettings(string key)
        {
            try
            {
                var appSetting = ConfigurationManager.AppSettings;
                return appSetting[key] ?? string.Empty; // Devuelve valor de la key. Si no existe,
                                                        // devuelve un String vacio (es como if)

            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error leyendo archivo de configuracion");
                return string.Empty;
            }

        }
    }
}

