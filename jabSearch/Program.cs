using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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





                    helper.RunAvailabilityCheckByDistrictLoop();

                    //helper.RunAvailabilityCheckByPinLoop();





                  
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
