using System.Threading.Tasks;
using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using BTCPayServer.Configuration;
using BTCPayServer.HostedServices;
using BTCPayServer.Lightning;
using BTCPayServer.Security;
using BTCPayServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServer.Controllers.GreenField
{
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
    [LightningUnavailableExceptionFilter]
    public class InternalLightningNodeApiController : LightningNodeApiController
    {
        private readonly BTCPayServerOptions _btcPayServerOptions;
        private readonly BTCPayNetworkProvider _btcPayNetworkProvider;
        private readonly LightningClientFactoryService _lightningClientFactory;


        public InternalLightningNodeApiController(BTCPayServerOptions btcPayServerOptions,
            BTCPayNetworkProvider btcPayNetworkProvider, BTCPayServerEnvironment btcPayServerEnvironment,
            CssThemeManager cssThemeManager, LightningClientFactoryService lightningClientFactory) : base(
            btcPayNetworkProvider, btcPayServerEnvironment, cssThemeManager)
        {
            _btcPayServerOptions = btcPayServerOptions;
            _btcPayNetworkProvider = btcPayNetworkProvider;
            _lightningClientFactory = lightningClientFactory;
        }

        [Authorize(Policy = Policies.CanUseInternalLightningNode,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpGet("~/api/v1/server/lightning/{bitcoinCode}/info")]
        public override Task<IActionResult> GetInfo(string bitcoinCode)
        {
            return base.GetInfo(bitcoinCode);
        }

        [Authorize(Policy = Policies.CanUseInternalLightningNode,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpPost("~/api/v1/server/lightning/{bitcoinCode}/connect")]
        public override Task<IActionResult> ConnectToNode(string bitcoinCode, ConnectToNodeRequest request)
        {
            return base.ConnectToNode(bitcoinCode, request);
        }

        [Authorize(Policy = Policies.CanUseInternalLightningNode,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpGet("~/api/v1/server/lightning/{bitcoinCode}/channels")]
        public override Task<IActionResult> GetChannels(string bitcoinCode)
        {
            return base.GetChannels(bitcoinCode);
        }

        [Authorize(Policy = Policies.CanUseInternalLightningNode,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpPost("~/api/v1/server/lightning/{bitcoinCode}/channels")]
        public override Task<IActionResult> OpenChannel(string bitcoinCode, OpenLightningChannelRequest request)
        {
            return base.OpenChannel(bitcoinCode, request);
        }

        [Authorize(Policy = Policies.CanUseInternalLightningNode,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpPost("~/api/v1/server/lightning/{bitcoinCode}/address")]
        public override Task<IActionResult> GetDepositAddress(string bitcoinCode)
        {
            return base.GetDepositAddress(bitcoinCode);
        }
        
        [Authorize(Policy = Policies.CanUseInternalLightningNode,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpGet("~/api/v1/server/lightning/{bitcoinCode}/invoices/{id}")]
        public override Task<IActionResult> GetInvoice(string bitcoinCode, string id)
        {
            return base.GetInvoice(bitcoinCode, id);
        }

        [Authorize(Policy = Policies.CanUseInternalLightningNode,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpPost("~/api/v1/server/lightning/{bitcoinCode}/invoices/pay")]
        public override Task<IActionResult> PayInvoice(string bitcoinCode, PayLightningInvoiceRequest lightningInvoice)
        {
            return base.PayInvoice(bitcoinCode, lightningInvoice);
        }

        [Authorize(Policy = Policies.CanCreateLightningInvoiceInternalNode,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpPost("~/api/v1/server/lightning/{bitcoinCode}/invoices")]
        public override Task<IActionResult> CreateInvoice(string bitcoinCode, CreateLightningInvoiceRequest request)
        {
            return base.CreateInvoice(bitcoinCode, request);
        }

        protected override Task<ILightningClient> GetLightningClient(string bitcoinCode, bool doingAdminThings)
        {
            _btcPayServerOptions.InternalLightningBybitcoinCode.TryGetValue(bitcoinCode,
                out var internalLightningNode);
            var network = _btcPayNetworkProvider.GetNetwork<BTCPayNetwork>(bitcoinCode);
            if (network == null || !CanUseInternalLightning(doingAdminThings) || internalLightningNode == null)
            {
                return null;
            }

            return Task.FromResult(_lightningClientFactory.Create(internalLightningNode, network));
        }
    }
}
