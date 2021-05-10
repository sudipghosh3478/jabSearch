using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace jabSearch
{
    class OTPHelper
    {
        internal void GenerateOTP()
        {
            string generateOTPURL = "https://cdn-api.co-vin.in/api/v2/auth/generateMobileOTP";
            string validateOTPURL = "https://cdn-api.co-vin.in/api/v2/auth/validateMobileOTP";



            var client = new HttpClient();
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(generateOTPURL),
                Method = HttpMethod.Post,

            };
            request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36");

            request.Content = JsonContent.Create(new { mobile = "**********", secret = "U2FsdGVkX1/T9BeDN57pl0X+sNYjJtpEyjWvhsabrNk5zlCTZUkbr6vndlj7HfcGGoAz+9QqJpZTKHv5CyybNA==" });
            Console.WriteLine("Sending OTP request");
            HttpResponseMessage APIResponse = client.SendAsync(request).Result;

            dynamic response = JObject.Parse(APIResponse.Content.ReadAsStringAsync().Result);
            var transactionID = response.txnId.ToString();
            Console.WriteLine("transaction id " + transactionID);
            Console.WriteLine("Enter OTP");
            string OTP = Console.ReadLine();

            string encryptedOTP = ComputeSha256Hash(OTP);

            request = new HttpRequestMessage()
            {
                RequestUri = new Uri(validateOTPURL),
                Method = HttpMethod.Post,
            };
            request.Content = JsonContent.Create(new { otp = encryptedOTP, txnId = transactionID });
            request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36");
            APIResponse = client.SendAsync(request).Result;

            response = JObject.Parse(APIResponse.Content.ReadAsStringAsync().Result);

            var token = response.token.ToString();

            var filePath = @"C:\Users\sudip_q923fws\source\repos\jabSearch\jabSearch\bin\Debug\token1.txt";
            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var writer = new StreamWriter(filePath))
            {
                writer.Write(token);
                writer.Flush();
            }

        }

        public static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
