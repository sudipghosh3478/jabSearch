using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace jabSearch
{
    class MessageHelper
    {
        HttpClient _messageClient;

      
        public HttpClient MaessageClient
        {
            get
            {
                _messageClient = _messageClient == null ? new HttpClient() : _messageClient;
                return _messageClient;
            }
        }

        const string callMeBotBaseUrl = "https://api.callmebot.com/whatsapp.php";
        const string callMeBotKey = "******";
        const string phone = "***********";

        internal void SendErrorMessage(string message)
        {
            SendMessage("Error: " + message);
        }



        internal void SendMessage(string message)
        {
            //Console.WriteLine(message);
           
            string callMeBotUrl = $"{callMeBotBaseUrl }?phone={phone}&apikey={callMeBotKey}&text={ message.Trim().Replace(' ', '+')}";
            MaessageClient.GetAsync(callMeBotUrl);
            //var response = MaessageClient.GetAsync(callMeBotUrl).Result;
        }

    }
}
