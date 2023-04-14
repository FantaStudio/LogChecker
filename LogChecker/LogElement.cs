using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogChecker
{
    public class LogElement
    {
        public DateTime Timestamp { get; set; }

        public string Level { get; set; }

        public string MessageTemplate { get; set; }

        public string Exception { get; set; }

        public Properties Properties { get; set; }

        public string GetApplicationName()
        {
            return Properties.MachineName switch
            {
                "uc-dl-api-test" => "Lanit.UCFK.KAPI.Int",
                "uc-pz-int" => "Lanit.UCFK.Integration.FZS",
                "uc-dl-web-1" => "Lanit.UCFK.KDL",
                "uc-pz-web-lk" => "Lanit.UCFK.Portal.FZS ЗЧ",
                "uc-pz-web-pub" => "Lanit.UCFK.Portal.FZS ОЧ",
                "uc-vrs-web-lk" => "Lanit.UCFK.Portal",
                "uc-pz-app" => "Lanit.UCFK.Scheduler.FZS",
                "uc-vrs-app-1" => "Lanit.UCFK.Scheduler App 1",
                "uc-vrs-app-2" => "Lanit.UCFK.Scheduler App 2",
                "uc-vrs-app-3" => "Lanit.UCFK.Scheduler App 3",
                _ => Properties.MachineName,
            };
        }
    }

    public class Properties
    {
        public EventId EventId { get; set; }

        public string SourceContext { get; set; }

        public string RequestId { get; set; }

        public string RequestPath { get; set; }

        public string SpanId { get; set; }

        public string TraceId { get; set; }

        public string ParentId { get; set; }

        public string ConnectionId { get; set; }

        public string MachineName { get; set; }

        public string Application { get; set; }
    }

    public class EventId
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
