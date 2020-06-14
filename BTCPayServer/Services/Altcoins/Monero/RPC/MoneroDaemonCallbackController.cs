using BTCPayServer.Filters;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServer.Services.Altcoins.Monero.RPC
{
    [Route("[controller]")]
    [OnlyIfSupportAttribute("XMR")]
    public class MoneroLikeDaemonCallbackController : Controller
    {
        private readonly EventAggregator _eventAggregator;

        public MoneroLikeDaemonCallbackController(EventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }
        [HttpGet("block")]
        public IActionResult OnBlockNotify(string hash, string bitcoinCode)
        {
            _eventAggregator.Publish(new MoneroEvent()
            {
                BlockHash = hash,
                bitcoinCode = bitcoinCode.ToUpperInvariant()
            });
            return Ok();
        }
        [HttpGet("tx")]
        public IActionResult OnTransactionNotify(string hash, string bitcoinCode)
        {
            _eventAggregator.Publish(new MoneroEvent()
            {
                TransactionHash = hash,
                bitcoinCode = bitcoinCode.ToUpperInvariant()
            });
            return Ok();
        }

    }
}
