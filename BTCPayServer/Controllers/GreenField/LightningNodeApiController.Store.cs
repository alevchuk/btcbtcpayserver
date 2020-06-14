using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using BTCPayServer.Configuration;
using BTCPayServer.Data;
using BTCPayServer.HostedServices;
using BTCPayServer.Lightning;
using BTCPayServer.Payments;
using BTCPayServer.Payments.Lightning;
using BTCPayServer.Security;
using BTCPayServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServer.Controllers.GreenField
{
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
    [LightningUnavailableExceptionFilter]
    public class StoreLightningNodeApiController : LightningNodeApiController
    {
        private readonly BTCPayServerOptions _btcPayServerOptions;
        private readonly LightningClientFactoryService _lightningClientFactory;
        private readonly BTCPayNetworkProvider _btcPayNetworkProvider;

        public StoreLightningNodeApiController(
            BTCPayServerOptions btcPayServerOptions,
            LightningClientFactoryService lightningClientFactory, BTCPayNetworkProvider btcPayNetworkProvider,
            BTCPayServerEnvironment btcPayServerEnvironment, CssThemeManager cssThemeManager) : base(
            btcPayNetworkProvider, btcPayServerEnvironment, cssThemeManager)
        {
            _btcPayServerOptions = btcPayServerOptions;
            _lightningClientFactory = lightningClientFactory;
            _btcPayNetworkProvider = btcPayNetworkProvider;
        }
        [Authorize(Policy = Policies.CanUseLightningNodeInStore,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpGet("~/api/v1/stores/{storeId}/lightning/{bitcoinCode}/info")]
        public override Task<IActionResult> GetInfo(string bitcoinCode)
        {
            return base.GetInfo(bitcoinCode);
        }

        [Authorize(Policy = Policies.CanUseLightningNodeInStore,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpPost("~/api/v1/stores/{storeId}/lightning/{bitcoinCode}/connect")]
        public override Task<IActionResult> ConnectToNode(string bitcoinCode, ConnectToNodeRequest request)
        {
            return base.ConnectToNode(bitcoinCode, request);
        }
        [Authorize(Policy = Policies.CanUseLightningNodeInStore,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpGet("~/api/v1/stores/{storeId}/lightning/{bitcoinCode}/channels")]
        public override Task<IActionResult> GetChannels(string bitcoinCode)
        {
            return base.GetChannels(bitcoinCode);
        }
        [Authorize(Policy = Policies.CanUseLightningNodeInStore,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpPost("~/api/v1/stores/{storeId}/lightning/{bitcoinCode}/channels")]
        public override Task<IActionResult> OpenChannel(string bitcoinCode, OpenLightningChannelRequest request)
        {
            return base.OpenChannel(bitcoinCode, request);
        }

        [Authorize(Policy = Policies.CanUseLightningNodeInStore,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpPost("~/api/v1/stores/{storeId}/lightning/{bitcoinCode}/address")]
        public override Task<IActionResult> GetDepositAddress(string bitcoinCode)
        {
            return base.GetDepositAddress(bitcoinCode);
        }

        [Authorize(Policy = Policies.CanUseLightningNodeInStore,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpPost("~/api/v1/stores/{storeId}/lightning/{bitcoinCode}/invoices/pay")]
        public override Task<IActionResult> PayInvoice(string bitcoinCode, PayLightningInvoiceRequest lightningInvoice)
        {
            return base.PayInvoice(bitcoinCode, lightningInvoice);
        }

        [Authorize(Policy = Policies.CanUseLightningNodeInStore,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpGet("~/api/v1/stores/{storeId}/lightning/{bitcoinCode}/invoices/{id}")]
        public override Task<IActionResult> GetInvoice(string bitcoinCode, string id)
        {
            return base.GetInvoice(bitcoinCode, id);
        }

        [Authorize(Policy = Policies.CanCreateLightningInvoiceInStore,
            AuthenticationSchemes = AuthenticationSchemes.Greenfield)]
        [HttpPost("~/api/v1/stores/{storeId}/lightning/{bitcoinCode}/invoices")]
        public override Task<IActionResult> CreateInvoice(string bitcoinCode, CreateLightningInvoiceRequest request)
        {
            return base.CreateInvoice(bitcoinCode, request);
        }

        protected override Task<ILightningClient> GetLightningClient(string bitcoinCode,
            bool doingAdminThings)
        {
            _btcPayServerOptions.InternalLightningBybitcoinCode.TryGetValue(bitcoinCode,
                out var internalLightningNode);
            var network = _btcPayNetworkProvider.GetNetwork<BTCPayNetwork>(bitcoinCode);

            var store = HttpContext.GetStoreData();
            if (network == null || store == null)
            {
                return null;
            }

            var id = new PaymentMethodId(bitcoinCode, PaymentTypes.LightningLike);
            var existing = store.GetSupportedPaymentMethods(_btcPayNetworkProvider)
                .OfType<LightningSupportedPaymentMethod>()
                .FirstOrDefault(d => d.PaymentId == id);
            if (existing == null || (existing.GetLightningUrl().IsInternalNode(internalLightningNode) &&
                                     !CanUseInternalLightning(doingAdminThings)))
            {
                return null;
            }

            return Task.FromResult(_lightningClientFactory.Create(existing.GetLightningUrl(), network));
        }
    }
}
