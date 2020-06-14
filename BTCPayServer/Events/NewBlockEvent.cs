using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BTCPayServer.Events
{
    public class NewBlockEvent
    {
        public string bitcoinCode { get; set; }
        public override string ToString()
        {
            return $"{bitcoinCode}: New block";
        }
    }
}
