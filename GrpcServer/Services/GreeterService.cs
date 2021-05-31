using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcServer
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;

        private List<Employee> _employees = new List<Employee>
        {
            new Employee()
            {
                Id = 0,
                BadgeNumber = 1,
                FirstName = "William",
                LastName = "Zhou",
                VacationAccrualRate = (float)0.5,
                VacationAccrued = 8
            },
            new Employee()
            {
                Id = 1,
                BadgeNumber = 2,
                FirstName = "Chales",
                LastName = "Onnes",
                VacationAccrualRate = (float)0.5,
                VacationAccrued = 8
            },
            new Employee()
            {
                Id = 2,
                BadgeNumber = 3,
                FirstName = "Evan",
                LastName = "Onnes",
                VacationAccrualRate = (float)0.5,
                VacationAccrued = 8
            }
        };

        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }

        public override Task<EmployeeResponse> GetByBadgeNumber(GetByBadgeNumberRequest request, ServerCallContext context)
        {
            var meta = context.RequestHeaders;
            Console.WriteLine();
            foreach (var entry in meta)
            {
                _logger.LogInformation($"{entry.Key} : {entry.Value}");
            }

            var employee = _employees.Single(e => e.BadgeNumber == request.BadgeNumber);

            return Task.FromResult(new EmployeeResponse()
            {
                Employee = employee
            });
        }

        public override async Task GetAll(GetAllRequest request, IServerStreamWriter<EmployeeResponse> responseStream, ServerCallContext context)
        {
            foreach (var e in _employees)
            {
                await responseStream.WriteAsync(new EmployeeResponse()
                {
                    Employee = e
                });
            }
        }

        public override async Task<AddPhotoResponse> AddPhoto(IAsyncStreamReader<AddPhotoRequest> requestStream, ServerCallContext context)
        {
            var meta = context.RequestHeaders;

            foreach (var entry in meta)
            {
                if (entry.Key.Equals("badgenumber", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    var bn = Convert.ToInt32(entry.Value);
                    var employee = _employees.Single(e => e.BadgeNumber == bn);
                    Console.WriteLine("Receiving the photo for " + employee.FirstName);

                    var data = new List<byte>();
                    while (await requestStream.MoveNext())
                    {
                        Console.WriteLine("Received " + requestStream.Current.Data.Length + " bytes");
                        data.AddRange(requestStream.Current.Data);
                    }

                    File.WriteAllBytes(@"C:\temp\" + employee.FirstName + ".jpg", data.ToArray());

                    return new AddPhotoResponse()
                    {
                        IsOK = true
                    };
                }
            }

            return new AddPhotoResponse()
            {
                IsOK = false
            };
        }

        public override async Task SaveAll(IAsyncStreamReader<EmployeeRequest> requestStream, IServerStreamWriter<EmployeeResponse> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var employee = requestStream.Current.Employee;
                lock (this)
                {
                    _employees.Add(employee);
                }

                await responseStream.WriteAsync(new EmployeeResponse()
                {
                    Employee = employee
                });

                _employees.ForEach(Console.WriteLine);
            }
        }
    }
}