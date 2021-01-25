using System;
using Consul;
using Microsoft.Extensions.Configuration;

namespace MicroService_Face_3_0.Utility
{
    public static class ConsulHelper
    {
        public static void ConsulRegist(this IConfiguration configuration)
        {
            ConsulClient client = new ConsulClient(c =>
            {
                c.Address = new Uri("http://localhost:5100/");
                c.Datacenter = "dc1";
            });

            //Get from command line
            //string ip = configuration["ip"];
            //int port = int.Parse(configuration["port"]);
            string ip = "127.0.0.1";
            int port = 5000;

            //Console.WriteLine($"ip= {ip}");
            //Console.WriteLine($"port = {port}");

            client.Agent.ServiceRegister(new AgentServiceRegistration()
            {
                ID = "service" + Guid.NewGuid(), //Only key
                Name = "FaceService",  //Team Name
                Address = ip,
                Port = port,
                Tags = null,
                Check = new AgentServiceCheck()
                {
                    Interval = TimeSpan.FromSeconds(10),
                    HTTP = $"http://{ip}:{port}/api/HealthCheck/Index",
                    Timeout = TimeSpan.FromSeconds(3),
                    DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(3) //从服务列表干掉
                }
            }) ;    
        }
    }
}
