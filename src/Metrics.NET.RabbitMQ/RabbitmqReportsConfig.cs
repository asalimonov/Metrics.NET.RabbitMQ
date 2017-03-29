using RMQ = RabbitMQ.Client;

namespace Metrics.RabbitMQ
{
    public class RabbitMQReportsConfig
    {
        #region Fields and properties
        public string HostName { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public string ExchangeType { get; set; }
        public string ExchangeName { get; set; }
        public bool ExchangeDurable { get; set; }
        public bool ExchangeAutoDelete { get; set; }
        public string RoutingKey { get; set; }
        public bool ReplaceDotsOnFieldNames { get; set; }
        public string VirtualHost { get; set; }
        public RMQ.IProtocol Protocol { get; set; }
        public bool ExchangePassive { get; set; }
        public RMQ.SslOption Ssl { get; set; }
        public string QueueName { get; set; }
        /**
        Connection timeout in seconds, recomnds to use 8-20 seconds
        **/
        public int ConnectionTimeout { get; set; }

        #endregion

        public RabbitMQReportsConfig()
        {
            HostName = "localhost";
            Port = 5672;
            UserName = "guest";
            Password = "guest";
            ExchangeType =  RMQ.ExchangeType.Topic;
            Protocol = RMQ.Protocols.DefaultProtocol;
            ExchangeName = "metrics";
            ExchangeDurable = false;
            ExchangeAutoDelete = true;
            ExchangePassive = false;
            RoutingKey = "metics";
            ReplaceDotsOnFieldNames = false;
            VirtualHost = "/";
            ConnectionTimeout = 10;
            Ssl = new RMQ.SslOption();
        }
    }
}