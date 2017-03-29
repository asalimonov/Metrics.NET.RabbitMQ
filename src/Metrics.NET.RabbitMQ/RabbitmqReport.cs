using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Metrics.Json;
using Metrics.MetricData;
using Metrics.Reporters;
using Metrics.Utils;
using RabbitMQ.Client;

namespace Metrics.RabbitMQ
{
    public class RabbitMQReport : BaseReport, IDisposable
    {
        #region Fields and properties
        private readonly bool _replaceDotsOnFieldNames;
        static readonly string HostName = System.Net.Dns.GetHostName();
        private readonly RabbitMQReportsConfig _reportConfig;
        private List<RmqDocument> _data;


        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        #endregion

        #region Constructors
        public RabbitMQReport(RabbitMQReportsConfig reportConfig)
        {
            _reportConfig = reportConfig;
            _replaceDotsOnFieldNames = reportConfig.ReplaceDotsOnFieldNames;
        }
        #endregion

        #region Public methods
        public void Initialize()
        {
            _connectionFactory = new ConnectionFactory
            {
                HostName = _reportConfig.HostName,
                UserName = _reportConfig.UserName,
                Password = _reportConfig.Password,
                AutomaticRecoveryEnabled = true,
                VirtualHost = _reportConfig.VirtualHost,
                Port = _reportConfig.Port,
                Protocol = _reportConfig.Protocol,
                Ssl = _reportConfig.Ssl,
                RequestedHeartbeat = 60,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(30),
                TopologyRecoveryEnabled = true,
                RequestedConnectionTimeout = (int) TimeSpan.FromSeconds(_reportConfig.ConnectionTimeout).TotalMilliseconds
            };

            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_reportConfig.ExchangeName, ExchangeType.Topic, _reportConfig.ExchangeDurable, _reportConfig.ExchangeAutoDelete, null);

            if (ExchangeType.Direct.Equals(_reportConfig.ExchangeType))
            {
                _channel.QueueDeclare(_reportConfig.QueueName);
            }
            else if (ExchangeType.Fanout.Equals(_reportConfig.ExchangeType))
            {
                _channel.ExchangeDeclare(_reportConfig.ExchangeName, ExchangeType.Fanout, _reportConfig.ExchangeDurable,
                    _reportConfig.ExchangeAutoDelete, null);
            }
            else if (ExchangeType.Topic.Equals(_reportConfig.ExchangeType))
            {
                _channel.ExchangeDeclare(_reportConfig.ExchangeName, ExchangeType.Topic, _reportConfig.ExchangeDurable,
                   _reportConfig.ExchangeAutoDelete, null);
            }
            else if (ExchangeType.Headers.Equals(_reportConfig.ExchangeType))
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new ArgumentException(string.Format("Unsupported Exchange type: {0}.", _reportConfig.ExchangeType));
            }

        }


        public void Dispose()
        {
            _channel.Close();
            if (_connection != null && _connection.IsOpen)
            {
                _connection.Close();
            }
            GC.SuppressFinalize(this);
        }
        #endregion

        ~RabbitMQReport()
        {
            _channel.Close();
            _connection.Close();
        }

        #region Protected methods
        protected override void StartReport(string contextName)
        {
            if (_connectionFactory == null)
            {
                Initialize();
            }

            _data = new List<RmqDocument>();
            base.StartReport(contextName);
        }

        protected override void EndReport(string contextName)
        {
            base.EndReport(contextName);

            var message = "{\"items\": [" + string.Join(",", _data.Select<RmqDocument, string>((Func<RmqDocument, string>)(d => d.ToJsonString()))) + "]}";
            _channel.BasicPublish(_reportConfig.ExchangeName, _reportConfig.RoutingKey, basicProperties: null, body: Encoding.UTF8.GetBytes(message));
        }

        protected override void ReportGauge(string name, double value, Unit unit, MetricTags tags)
        {
            if (!double.IsNaN(value) && !double.IsInfinity(value))
            {
                Pack("Gauge", name, unit, tags, new[] {
                    new JsonProperty("Value", value),
                });
            }
        }

        protected override void ReportCounter(string name, CounterValue value, Unit unit, MetricTags tags)
        {
            var itemProperties = value.Items.SelectMany(i => new[]
            {
                new JsonProperty(i.Item + " - Count", i.Count),
                new JsonProperty(i.Item + " - Percent", i.Percent),
            });

            Pack("Counter", name, unit, tags, new[] {
                new JsonProperty("Count", value.Count),
            }.Concat(itemProperties));
        }

        protected override void ReportMeter(string name, MeterValue value, Unit unit, TimeUnit rateUnit, MetricTags tags)
        {
            var itemProperties = value.Items.SelectMany(i => new[]
            {
                new JsonProperty(i.Item + " - Count", i.Value.Count),
                new JsonProperty(i.Item + " - Percent", i.Percent),
                new JsonProperty(i.Item + " - Mean Rate", i.Value.MeanRate),
                new JsonProperty(i.Item + " - 1 Min Rate", i.Value.OneMinuteRate),
                new JsonProperty(i.Item + " - 5 Min Rate", i.Value.FiveMinuteRate),
                new JsonProperty(i.Item + " - 15 Min Rate", i.Value.FifteenMinuteRate)
            });

            Pack("Meter", name, unit, tags, new[] {
                new JsonProperty("Count", value.Count),
                new JsonProperty("Mean Rate", value.MeanRate),
                new JsonProperty("1 Min Rate", value.OneMinuteRate),
                new JsonProperty("5 Min Rate", value.FiveMinuteRate),
                new JsonProperty("15 Min Rate", value.FifteenMinuteRate)
            }.Concat(itemProperties));
        }

        protected override void ReportHistogram(string name, HistogramValue value, Unit unit, MetricTags tags)
        {
            Pack("Histogram", name, unit, tags, new[] {
                new JsonProperty("Total Count",value.Count),
                new JsonProperty("Last", value.LastValue),
                new JsonProperty("Last User Value", value.LastUserValue),
                new JsonProperty("Min",value.Min),
                new JsonProperty("Min User Value",value.MinUserValue),
                new JsonProperty("Mean",value.Mean),
                new JsonProperty("Max",value.Max),
                new JsonProperty("Max User Value",value.MaxUserValue),
                new JsonProperty("StdDev",value.StdDev),
                new JsonProperty("Median",value.Median),
                new JsonProperty("Percentile 75%",value.Percentile75),
                new JsonProperty("Percentile 95%",value.Percentile95),
                new JsonProperty("Percentile 98%",value.Percentile98),
                new JsonProperty("Percentile 99%",value.Percentile99),
                new JsonProperty(AdjustDottedFieldNames("Percentile 99.9%"), value.Percentile999),
                new JsonProperty("Sample Size", value.SampleSize)
            });
        }

        protected override void ReportTimer(string name, TimerValue value, Unit unit, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
        {
            Pack("Timer", name, unit, tags, new[] {
                new JsonProperty("Total Count",value.Rate.Count),
                new JsonProperty("Active Sessions",value.ActiveSessions),
                new JsonProperty("Mean Rate", value.Rate.MeanRate),
                new JsonProperty("1 Min Rate", value.Rate.OneMinuteRate),
                new JsonProperty("5 Min Rate", value.Rate.FiveMinuteRate),
                new JsonProperty("15 Min Rate", value.Rate.FifteenMinuteRate),
                new JsonProperty("Last", value.Histogram.LastValue),
                new JsonProperty("Last User Value", value.Histogram.LastUserValue),
                new JsonProperty("Min",value.Histogram.Min),
                new JsonProperty("Min User Value",value.Histogram.MinUserValue),
                new JsonProperty("Mean",value.Histogram.Mean),
                new JsonProperty("Max",value.Histogram.Max),
                new JsonProperty("Max User Value",value.Histogram.MaxUserValue),
                new JsonProperty("StdDev",value.Histogram.StdDev),
                new JsonProperty("Median",value.Histogram.Median),
                new JsonProperty("Percentile 75%",value.Histogram.Percentile75),
                new JsonProperty("Percentile 95%",value.Histogram.Percentile95),
                new JsonProperty("Percentile 98%",value.Histogram.Percentile98),
                new JsonProperty("Percentile 99%",value.Histogram.Percentile99),
                new JsonProperty(AdjustDottedFieldNames("Percentile 99.9%"), value.Histogram.Percentile999),
                new JsonProperty("Sample Size", value.Histogram.SampleSize)
            });
        }

        private string AdjustDottedFieldNames(string fieldName)
        {
            return _replaceDotsOnFieldNames ? fieldName.Replace(".", "_") : fieldName;
        }

        protected override void ReportHealth(HealthStatus status)
        {
            var props = new List<JsonProperty>{
                new JsonProperty("IsHealthy", status.IsHealthy),
                new JsonProperty("RegisteredChecksCount", status.Results.Count())
            };

            List<JsonObject> checks = new List<JsonObject>();
            foreach (var healthResult in status.Results)
            {
                checks.Add(new JsonObject(
                    new[] {
                        new JsonProperty("Check",healthResult.Name),
                        new JsonProperty("IsHealthy",healthResult.Check.IsHealthy),
                        new JsonProperty("Message",healthResult.Check.Message) }));
            }
            props.Add(new JsonProperty("HealthChecks", checks));

            Pack("Health", "HealthStatus", Unit.None, MetricTags.None, props);
        }
        #endregion

        #region Private methods and classes
        private void Pack(string type, string name, Unit unit, MetricTags tags, IEnumerable<JsonProperty> properties)
        {
            _data.Add(new RmqDocument
            {
                Type = type,
                Object = new JsonObject(new[] {
                    new JsonProperty("Timestamp", Clock.FormatTimestamp(this.CurrentContextTimestamp)),
                    new JsonProperty("Type",type),
                    new JsonProperty("Name",name),
                    new JsonProperty("ServerName",HostName),
                    new JsonProperty("Unit", unit.ToString()),
                    new JsonProperty("Tags", tags.Tags)
                }.Concat(properties))
            });
        }


        private class RmqDocument
        {
            public string Type { get; set; }
            public JsonObject Object { get; set; }

            public string ToJsonString()
            {
                return Object.AsJson(false, 0);
            }
        }
        #endregion
    }
}