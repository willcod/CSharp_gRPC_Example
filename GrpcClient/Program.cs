using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcServer;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using EmployeeRequest = GrpcServer.EmployeeRequest;
using Employee = GrpcServer.Employee;

namespace GrpcClient
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var httpHandler = new HttpClientHandler();
            // Return `true` to allow certificates that are untrusted/invalid
            httpHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            var channel = GrpcChannel.ForAddress("https://127.0.0.1:5001", new GrpcChannelOptions()
            {
                HttpHandler = httpHandler
            });
            var client = new Greeter.GreeterClient(channel);

            switch ("5"/*args[1]*/)
            {
                case "1":
                    var reply = client.SayHello(new HelloRequest()
                    {
                        Name = "World"
                    });
                    Console.WriteLine();

                    Console.WriteLine($"Reply from server:\n{reply.Message}");
                    break;

                case "2":
                    var response = client.GetByBadgeNumber(new GrpcServer.GetByBadgeNumberRequest()
                    {
                        BadgeNumber = 1
                    });
                    Console.WriteLine(response.Employee);
                    break;

                case "3":
                    GetAllAsync(client).Wait();
                    using (var res = client.GetAll(new GrpcServer.GetAllRequest()))
                    {
                    }
                    break;

                case "4":
                    AddPhotoAsync(client).Wait();
                    break;
                case "5":
                    SaveAllAsync(client).Wait();
                    break;
                default:
                    break;
            }

            Console.ReadKey();
        }

        private static async Task SaveAllAsync(Greeter.GreeterClient client)
        {
            var employees = new List<GrpcServer.Employee>()
            {
                new GrpcServer.Employee()
                {
                    BadgeNumber = 123,
                    FirstName = "John",
                    LastName = "Smith",
                    VacationAccrualRate = 1.2f,
                    VacationAccrued = 0
                },
                new GrpcServer.Employee()
                {
                    BadgeNumber = 124,
                    FirstName = "Lisa",
                    LastName = "Wu",
                    VacationAccrualRate = 1.7f,
                    VacationAccrued = 10
                }
            };

            using var call = client.SaveAll();
            var responseStream = call.ResponseStream;
            var requestStream = call.RequestStream;

            var responseTask = Task.Run(async () =>
            {
                while (await responseStream.MoveNext())
                {
                    Console.WriteLine("Saved " + responseStream.Current.Employee);
                }
            });

            foreach (var employee in employees)
            {

                await requestStream.WriteAsync(new GrpcServer.EmployeeRequest()
                {
                    Employee = employee
                });
            }

            await call.RequestStream.CompleteAsync();
            await responseTask;
        }

        private static async Task AddPhotoAsync(Greeter.GreeterClient client)
        {
            var md = new Metadata { { "badgeNumber", "2" } };

            var fs = File.OpenRead(@"C:\temp\save.jpg");
            using var call = client.AddPhoto(md);
            var stream = call.RequestStream;
            while (true)
            {
                byte[] buffer = new byte[64 * 1024];
                int numRead = await fs.ReadAsync(buffer, 0, buffer.Length);
                if (numRead == 0) break;

                if (numRead < buffer.Length)
                {
                    Array.Resize(ref buffer, numRead);
                }

                await stream.WriteAsync(new GrpcServer.AddPhotoRequest()
                {
                    Data = ByteString.CopyFrom(buffer)
                });
            }

            await stream.CompleteAsync();

            var res = await call.ResponseAsync;
            Console.WriteLine(res.IsOK);
        }

        private static async Task GetAllAsync(Greeter.GreeterClient client)
        {
            using var call = client.GetAll(new GrpcServer.GetAllRequest());
            var stream = call.ResponseStream;
            while (await stream.MoveNext())
            {
                Console.WriteLine(stream.Current.Employee);
            }
        }
    }
}