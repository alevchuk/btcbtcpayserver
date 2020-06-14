using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Client.Models;
using BTCPayServer.Lightning;

namespace BTCPayServer.Client
{
    public partial class BTCPayServerClient
    {
        public async Task<LightningNodeInformationData> GetLightningNodeInfo(string storeId, string bitcoinCode,
            CancellationToken token = default)
        {
            var response = await _httpClient.SendAsync(
                CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{bitcoinCode}/info",
                    method: HttpMethod.Get), token);
            return await HandleResponse<LightningNodeInformationData>(response);
        }

        public async Task ConnectToLightningNode(string storeId, string bitcoinCode, ConnectToNodeRequest request,
            CancellationToken token = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var response = await _httpClient.SendAsync(
                CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{bitcoinCode}/connect", bodyPayload: request,
                    method: HttpMethod.Post), token);
            await HandleResponse(response);
        }

        public async Task<IEnumerable<LightningChannelData>> GetLightningNodeChannels(string storeId, string bitcoinCode,
            CancellationToken token = default)
        {
            var response = await _httpClient.SendAsync(
                CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{bitcoinCode}/channels",
                    method: HttpMethod.Get), token);
            return await HandleResponse<IEnumerable<LightningChannelData>>(response);
        }

        public async Task<string> OpenLightningChannel(string storeId, string bitcoinCode, OpenLightningChannelRequest request,
            CancellationToken token = default)
        {
            var response = await _httpClient.SendAsync(
                CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{bitcoinCode}/channels", bodyPayload: request,
                    method: HttpMethod.Post), token);
            return await HandleResponse<string>(response);
        }

        public async Task<string> GetLightningDepositAddress(string storeId, string bitcoinCode,
            CancellationToken token = default)
        {
            var response = await _httpClient.SendAsync(
                CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{bitcoinCode}/address", method: HttpMethod.Post),
                token);
            return await HandleResponse<string>(response);
        }

        public async Task PayLightningInvoice(string storeId, string bitcoinCode, PayLightningInvoiceRequest request,
            CancellationToken token = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var response = await _httpClient.SendAsync(
                CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{bitcoinCode}/invoices/pay", bodyPayload: request,
                    method: HttpMethod.Post), token);
            await HandleResponse(response);
        }

        public async Task<LightningInvoiceData> GetLightningInvoice(string storeId, string bitcoinCode,
            string invoiceId, CancellationToken token = default)
        {
            if (invoiceId == null)
                throw new ArgumentNullException(nameof(invoiceId));
            var response = await _httpClient.SendAsync(
                CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{bitcoinCode}/invoices/{invoiceId}",
                    method: HttpMethod.Get), token);
            return await HandleResponse<LightningInvoiceData>(response);
        }

        public async Task<LightningInvoiceData> CreateLightningInvoice(string storeId, string bitcoinCode,
            CreateLightningInvoiceRequest request, CancellationToken token = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var response = await _httpClient.SendAsync(
                CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{bitcoinCode}/invoices", bodyPayload: request,
                    method: HttpMethod.Post), token);
            return await HandleResponse<LightningInvoiceData>(response);
        }
    }
}
