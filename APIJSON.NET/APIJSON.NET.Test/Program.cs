using JsonApiFramework.Server;
using RestSharp;
using System;

namespace APIJSON.NET.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new RestClient("http://localhost:5000/");
            var request = new RestRequest("get", Method.POST);
            request.AddJsonBody(@"{
                            'User': {
                                'id': 38710
                            }
                        }
                        ");
            IRestResponse response = client.Execute(request);
            Console.WriteLine(response.Content);

            request = new RestRequest("get", Method.POST);
            request.AddJsonBody(@"{
                'User': {
                    'id': 38710
                },
                '[]': {
                    'page': 0,
                    'count': 3,
                    'Moment': {
                        'userId': 38710
                    },
                    'Comment[]': {
                        'count': 3,
                        'Comment': {
                            'momentId@': '[]/Moment/id'
                        }
                    }
                }
            }
                                    ");
            IRestResponse response2 = client.Execute(request);
            Console.WriteLine(response2.Content);

            
            Console.ReadLine();
        }
    }
}
