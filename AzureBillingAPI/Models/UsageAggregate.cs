using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureBillingAPI.Models
{
    public class UsageAggregate
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public Properties properties { get; set; }
    }
}
