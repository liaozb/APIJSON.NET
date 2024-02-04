using RestSharp;
using System;
 

namespace APIJSON.NET.Test;

class Program
{
    static void Main(string[] args)
    {
        var client = new RestClient("http://localhost:5000/");

        var login = new RestRequest("token");
        login.Method= Method.Post;
        login.AddJsonBody(new TokenInput() { username = "admin1", password = "123456" });
        var token = client.Post<TokenData>(login);

        Console.WriteLine(token.data.AccessToken);

        var request = new RestRequest("get");
        request.Method = Method.Post;
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", "Bearer " + token.data.AccessToken);
        request.AddJsonBody(@"{
                            'User': {
                                'id': 38710
                            }
                        }
                        ");
        var response = client.Execute(request);
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
