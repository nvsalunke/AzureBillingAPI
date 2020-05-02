using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureBillingAPI.Models
{
    public class UsagePayload
    {
        public List<UsageAggregate> value { get; set; }
    }
}
