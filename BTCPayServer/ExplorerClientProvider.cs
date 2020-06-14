using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Configuration;
using BTCPayServer.Logging;
using NBXplorer;
using BTCPayServer.HostedServices;

namespace BTCPayServer
{
    public class ExplorerClientProvider
    {
        BTCPayNetworkProvider _NetworkProviders;
        BTCPayServerOptions _Options;

        public BTCPayNetworkProvider NetworkProviders => _NetworkProviders;
        NBXplorerDashboard _Dashboard;
        public ExplorerClientProvider(IHttpClientFactory httpClientFactory, BTCPayNetworkProvider networkProviders, BTCPayServerOptions options, NBXplorerDashboard dashboard)
        {
            _Dashboard = dashboard;
            _NetworkProviders = networkProviders;
            _Options = options;

            foreach (var setting in options.NBXplorerConnectionSettings)
            {
                var cookieFile = setting.CookieFile;
                if (cookieFile.Trim() == "0" || string.IsNullOrEmpty(cookieFile.Trim()))
                    cookieFile = null;
                Logs.Configuration.LogInformation($"{setting.bitcoinCode}: Explorer url is {(setting.ExplorerUri.AbsoluteUri ?? "not set")}");
                Logs.Configuration.LogInformation($"{setting.bitcoinCode}: Cookie file is {(setting.CookieFile ?? "not set")}");
                if (setting.ExplorerUri != null)
                {
                    _Clients.TryAdd(setting.bitcoinCode.ToUpperInvariant(), CreateExplorerClient(httpClientFactory.CreateClient(nameof(ExplorerClientProvider)), _NetworkProviders.GetNetwork<BTCPayNetwork>(setting.bitcoinCode), setting.ExplorerUri, setting.CookieFile));
                }
            }
        }

        private static ExplorerClient CreateExplorerClient(HttpClient httpClient, BTCPayNetwork n, Uri uri, string cookieFile)
        {

            var explorer = n.NBXplorerNetwork.CreateExplorerClient(uri);
            explorer.SetClient(httpClient);
            if (cookieFile == null)
            {
                Logs.Configuration.LogWarning($"{explorer.bitcoinCode}: Not using cookie authentication");
                explorer.SetNoAuth();
            }
            if(!explorer.SetCookieAuth(cookieFile))
            {
                Logs.Configuration.LogWarning($"{explorer.bitcoinCode}: Using cookie auth against NBXplorer, but {cookieFile} is not found");
            }
            return explorer;
        }

        Dictionary<string, ExplorerClient> _Clients = new Dictionary<string, ExplorerClient>();

        public ExplorerClient GetExplorerClient(string bitcoinCode)
        {
            var network = _NetworkProviders.GetNetwork<BTCPayNetwork>(bitcoinCode);
            if (network == null)
                return null;
            _Clients.TryGetValue(network.NBXplorerNetwork.bitcoinCode, out ExplorerClient client);
            return client;
        }

        public ExplorerClient GetExplorerClient(BTCPayNetworkBase network)
        {
            if (network == null)
                throw new ArgumentNullException(nameof(network));
            return GetExplorerClient(network.bitcoinCode);
        }

        public bool IsAvailable(BTCPayNetworkBase network)
        {
            return IsAvailable(network.bitcoinCode);
        }

        public bool IsAvailable(string bitcoinCode)
        {
            bitcoinCode = bitcoinCode.ToUpperInvariant();
            return _Clients.ContainsKey(bitcoinCode) && _Dashboard.IsFullySynched(bitcoinCode, out var unused);
        }

        public BTCPayNetwork GetNetwork(string bitcoinCode)
        {
            var network = _NetworkProviders.GetNetwork<BTCPayNetwork>(bitcoinCode);
            if (network == null)
                return null;
            if (_Clients.ContainsKey(network.NBXplorerNetwork.bitcoinCode))
                return network;
            return null;
        }

        public IEnumerable<(BTCPayNetwork, ExplorerClient)> GetAll()
        {
            foreach (var net in _NetworkProviders.GetAll().OfType<BTCPayNetwork>())
            {
                if (_Clients.TryGetValue(net.NBXplorerNetwork.bitcoinCode, out ExplorerClient explorer))
                {
                    yield return (net, explorer);
                }
            }
        }
    }
}
