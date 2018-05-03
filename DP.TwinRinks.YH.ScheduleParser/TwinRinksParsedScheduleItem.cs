using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DP.TwinRinks.YH.ScheduleParser
{
    public class TwinRinksParsedScheduleItem
    {
        public string Date { get; set; }
        public string Day { get; set; }
        public string Rink { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string Home { get; set; }
        public string Away { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
