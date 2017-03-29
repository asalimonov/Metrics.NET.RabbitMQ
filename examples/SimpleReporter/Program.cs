using System;
using Metrics;
using Metrics.RabbitMQ;


namespace SimpleReporter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Simple Metrics.NET RabbitMQ Reporter");
            var healthCheckEndpoint = "http://*:8866/";

            Metric.Config
                .WithHttpEndpoint(healthCheckEndpoint)
                .WithAllCounters()
                .WithInternalMetrics()
                .WithReporting(config => config
                    .WithRabbitMQ(new RabbitMQReportsConfig
                    {
                        HostName = "localhost",
                        Password = "guest",
                        UserName = "guest"

                    }, TimeSpan.FromSeconds(3)));

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();

            Metric.ShutdownContext("System");

        }
    }
}
