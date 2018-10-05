using RestSharp;
using System;

namespace APIJSON.NET.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new RestClient("http://localhost:5000/");

            var login = new RestRequest("token", Method.POST);
            login.AddJsonBody(new TokenInput() { username = "admin1", password = "123456" });
            IRestResponse<TokenData> token = client.Execute<TokenData>(login);

            Console.WriteLine(token.Data.data.AccessToken);

            var request = new RestRequest("get", Method.POST);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", "Bearer " + token.Data.data.AccessToken);
            request.AddJsonBody(@"{
                            'User': {
                                'id': 38710
                            }
                        }
                        ");
            IRestResponse response = client.Execute(request);
            Console.WriteLine(response.Content);
 
       


            Console.ReadLine();
        }
    }
    public class TokenInput
    {
        public string username { get; set; }
        public string password { get; set; }
    }
    public class TokenData
    {
        public AuthenticateResultModel data { get; set; }
    }
    public class AuthenticateResultModel
    {
        public string AccessToken { get; set; }

        public int ExpireInSeconds { get; set; }


    }
}
