# Metrics.NET.RabbitMQ
RabbitMQ reporter for [Metrics.NET](https://github.com/Recognos/Metrics.NET)

[Nuget](https://www.nuget.org/packages/Metrics.NET.RabbitMQ/) package.

Supports the followinging exchange types:
* topic
* fanout
* direct

Example:
```csharp
            Metric.Config
                .WithAllCounters()
                .WithInternalMetrics()
                .WithReporting(config => config
                    .WithRabbitMQ(new RabbitMQReportsConfig
                    {
                        HostName = "localhost",
                        Password = "guest",
                        UserName = "guest"

                    }, TimeSpan.FromSeconds(3)));
```
Metrics.NET sends all pre-registered CLR counters and internal metics to RabbitMQ with the default exchange type "topic" and it uses the default exchanege "metics" with "metics" routing key.
