using System;
using Metrics.Reports;

namespace Metrics.RabbitMQ
{

    #region Public methods

    public static class RabbitmqConfigExtensions
    {
        public static MetricsReports WithRabbitMQ(this MetricsReports reports, RabbitMQReportsConfig reportConfig, TimeSpan interval)
        {
            return reports.WithReport(new RabbitMQReport(reportConfig), interval);
        }
    }

    #endregion
}
