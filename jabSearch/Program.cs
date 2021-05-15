using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace jabSearch
{
    class Program
    {


        static void Main(string[] args)
        {
            try
            {

                using (JabHelper helper = new JabHelper())
                {



                    var pinSearch = ConfigurationManager.AppSettings["usePinSearch"];
                    if (pinSearch != "" && pinSearch.Trim().ToLowerInvariant() == "true")
                        helper.RunAvailabilityCheckByPinLoop();
                    else
                        helper.RunAvailabilityCheckByDistrictLoop();








                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);

            }
            Console.ReadKey();
        }



    }
}
