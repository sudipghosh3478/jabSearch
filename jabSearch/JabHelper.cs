using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace jabSearch
{
    public class JabHelper : IDisposable
    {



        readonly string baseUrl = System.Configuration.ConfigurationManager.AppSettings["baseUrl"];
        readonly string getSessionsForPuneUrlBase;          // "https://cdn-api.co-vin.in/api/v2/appointment/sessions/public/findByDistrict?district_id=363&date=";
        readonly string getSessionsByPinUrl;                   //https://cdn-api.co-vin.in/api/v2/appointment/sessions/public/findByPin?pincode=422027&date=10-05-2021
        readonly string scheduleAPIUrl;                                //"https://cdn-api.co-vin.in/api/v2/appointment/schedule";
        readonly string beneficiaries = System.Configuration.ConfigurationManager.AppSettings["beneficiaries"];                                  // "'29425896407850','49452312943680','18714828173000'";
        readonly string date = System.Configuration.ConfigurationManager.AppSettings["date"];
        readonly string day;
        string[] beneficiariesArr;

        string token;
        JwtSecurityTokenHandler TokenHandler = new JwtSecurityTokenHandler();

        HttpClient _apiClient;


        public HttpClient ApiClient
        {
            get
            {
                _apiClient = _apiClient == null ? new HttpClient() : _apiClient;
                return _apiClient;
            }
        }


        MessageHelper MessageHelper;
        Logger Logger;

        public JabHelper()
        {
            getSessionsForPuneUrlBase = baseUrl + System.Configuration.ConfigurationManager.AppSettings["getSessionsForPuneUrlBase"];
            getSessionsByPinUrl = baseUrl + System.Configuration.ConfigurationManager.AppSettings["getSessionsByPinUrl"];
            scheduleAPIUrl = baseUrl + System.Configuration.ConfigurationManager.AppSettings["scheduleAPIUrl"];

            beneficiariesArr = beneficiaries.Split(',');

            MessageHelper = new MessageHelper();
            Logger = new Logger();
            day = DateTime.Parse(date).Day.ToString();
        }



        void RunAvailabilityLoop(Action action)
        {
            try
            {

                Logger.Log("Running availability check for date  " + date);
                Logger.Log(action.Method.Name);
                Logger.EmptyLine();
                MessageHelper.SendMessage("Running availability check for date  " + date);
                while (true)
                {
                    // CheckToken();


                    action.Invoke();
                    int interval = getInterval();
                    System.Threading.Thread.Sleep(interval);
                };
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
                Logger.Log(ex.ToString());
            }
        }


        public void RunAvailabilityCheckByDistrictLoop()
        {
            RunAvailabilityLoop(RunAvailabilityCheckByDistrict);
        }

        public void RunAvailabilityCheckByPinLoop()
        {
            RunAvailabilityLoop(RunAvailabilityCheckByPin);
        }



        public void RunAvailabilityCheckByPin()
        {
            var pins = getPins().Split(',');
            List<Task> tasks = new List<Task>();
            foreach (var pin in pins)
            {
                tasks.Add(CheckByPin(pin));
            }
            Task.WaitAll(tasks.ToArray());
        }

        public Task CheckByPin(string pin)
        {
            return Task.Run(() =>
            {

                string getByPinUrl = $"{getSessionsByPinUrl}?pincode={pin}&date={date}";

                HttpResponseMessage pinSlotResponse = ApiClient.GetAsync(getByPinUrl).Result;
                ProcessSessionResponse(pinSlotResponse, pin);
            });

        }

        public void RunAvailabilityCheckByDistrict()
        {
            try
            {
                string getByDistrictUrl = $"{getSessionsForPuneUrlBase}&date={date}";

                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(getByDistrictUrl),
                    Method = HttpMethod.Get,

                };

                HttpResponseMessage districtSlotResponse = ApiClient.SendAsync(request).Result;

                /*
                 {
                       "sessions":[
                          {
                             "center_id":595533,
                             "name":"Primary Health Centre Kurkumbh",
                             "address":"Primary Health Centre Kurkumbh",
                             "state_name":"Maharashtra",
                             "district_name":"Pune",
                             "block_name":"Daund",
                             "pincode":413802,
                             "from":"12:00:00",
                             "to":"15:00:00",
                             "lat":18,
                             "long":74,
                             "fee_type":"Free",
                             "session_id":"e0d30dcf-80b2-4b9a-9ce2-257db1874579",
                             "date":"09-05-2021",
                             "available_capacity":1,
                             "fee":"0",
                             "min_age_limit":18,
                             "vaccine":"COVISHIELD",
                             "slots":[
                                "12:00PM-02:00PM",
                                "02:00PM-03:00PM"
                             ]
                          }
                       ]
                    }
                 * */
                if (districtSlotResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    ProcessSessionResponse(districtSlotResponse);
                else
                {
                    Logger.LogError(districtSlotResponse.StatusCode + " " + districtSlotResponse.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
                Logger.Log(ex.ToString());
            }
        }

        private void ProcessSessionResponse(HttpResponseMessage districtSlotResponse, string pin = "")
        {
            dynamic response = JObject.Parse(districtSlotResponse.Content.ReadAsStringAsync().Result);
            if (response.sessions != null)
            {
                var sessions = ((IEnumerable<dynamic>)response.sessions);
                if (response.sessions != null && sessions.Count() > 0)
                {
                    //var ssession = sessions.FirstOrDefault();
                    //SendSessionMessage(ssession);

                    var below45 = ((IEnumerable<dynamic>)response.sessions).Where(s => s.min_age_limit == 45);
                    if (below45.Count() > 0)
                    {
                        foreach (dynamic session in below45)
                        {
                            SendSessionMessage(session);

                            if (Convert.ToInt32(session.available_capacity.ToString()) >= beneficiariesArr.Length)
                            {
                                Console.Beep();
                                Console.Beep();
                                Logger.LogSuccess($"{session.ToString()}");
                                System.Threading.Thread.Sleep(1000);
                            }
                        }
                    }
                    else
                    {
                        Logger.Log("No Sessions available for above 45 on " + day + " " + pin);
                        //foreach (dynamic session in response.sessions)
                        //{
                        //    SendSessionMessage(session);
                        //    TryToBookAppointment(session);
                        //}
                    }
                }
                else
                {
                    Logger.Log("No Sessions available on date " + day + " " + pin);
                }
            }
            else
            {
                MessageHelper.SendErrorMessage(response.ToString());
            }
        }

        void SendSessionMessage(dynamic session)
        {
            var sessionString = $"{session.name.ToString()} \r\n{session.address.ToString()} \r\n Age:{session.min_age_limit} \r\nAvailable: {session.available_capacity.ToString()}";
            Logger.Log(sessionString);
            MessageHelper.SendMessage(sessionString);
        }


        internal void TryToBookAppointment(dynamic session)
        {
            CheckToken();

            var message = "Try for appointment \r\n" + $"{session.name.ToString()} \r\n{session.address.ToString()} \r\n Age:{session.min_age_limit} \r\nAvailable: {session.available_capacity.ToString()}";
            MessageHelper.SendMessage(message);
            Logger.Log(message);

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(scheduleAPIUrl),
                Method = HttpMethod.Post,
            };
            request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36");
            request.Headers.Add("authorization", "Bearer " + token);

            /*
             *{
               "dose": 1,
               "session_id": "98e411fd-ae79-429f-a1ab-084d8dca980a",
               "slot": "03:00PM-05:00PM",
               "beneficiaries": [
                 "49845773524320"
               ]
             }
             * */

            var lastSlot = ((Newtonsoft.Json.Linq.JArray)session.slots).Last.ToString();
            //string content = $@"{{ 
            //                        'dose': 1, 
            //                        'session_id': '{session.session_id}', 
            //                        'slot': '{slot}', 
            //                        'beneficiaries': [{beneficiaries}] 
            //                    }}";
            //request.Content = new StringContent(content);

            request.Content = JsonContent.Create(new { dose = 1, session_id = session.session_id, slot = lastSlot, beneficiaries = beneficiariesArr });

            HttpResponseMessage appointmentResponse = ApiClient.SendAsync(request).Result;

            if (appointmentResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                message = "Success \r\n" + appointmentResponse.ToString();
                MessageHelper.SendMessage(message);
                Logger.LogSuccess(message);
            }
            else
            {
                MessageHelper.SendErrorMessage(appointmentResponse.ToString());
                Logger.LogError(appointmentResponse.ToString());
                Logger.LogError(request.Content.ToString());
            }
        }


        void CheckToken()
        {
            try
            {
                if (token == null)
                    token = getToken();
                DateTime expiry = GetTokenExpiry(token);

                if (expiry < DateTime.Now)
                {
                    Logger.LogError("Token expired on " + expiry);
                    changeToken();
                }
                else if ((expiry - DateTime.Now).TotalMinutes < 5)
                {
                    Logger.LogWarning("Token will expire on " + expiry);
                    changeToken();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }

        }

        private DateTime GetTokenExpiry(string strToken)
        {
            var _token = TokenHandler.ReadJwtToken(strToken);
            var expiry = _token.ValidTo.ToLocalTime();
            return expiry;
        }

        void changeToken()
        {
            var newToken = getToken();
            if (token != newToken)
            {
                token = newToken;
                DateTime expiry = GetTokenExpiry(token);
                if (expiry > DateTime.Now)
                {
                    Logger.LogSuccess("Token valid till " + expiry);
                }

            }
        }


        string getToken()
        {
            using (StreamReader reader = new StreamReader("token.txt"))
            {
                return reader.ReadLine().Trim();
            }
        }

        string getPins()
        {
            using (StreamReader reader = new StreamReader("pins.txt"))
                return reader.ReadLine().Trim();
        }

        int getInterval()
        {
            //try
            //{
            //    using (StreamReader reader = new StreamReader("inerval.txt"))
            //        return Convert.ToInt32(reader.ReadLine().Trim());
            //}
            //catch
            //{
            //    return 5000;
            //}

            try
            {
                return Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["interval"]);
            }
            catch
            {
                return 30000;
            }
        }

        public void Dispose()
        {
            ApiClient.Dispose();
            this.Logger.Dispose();
        }
    }
}
