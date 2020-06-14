using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBXplorer;

namespace BTCPayServer
{
    public partial class BTCPayNetworkProvider
    {
        public void InitFeathercoin()
        {
            var nbxplorerNetwork = NBXplorerNetworkProvider.GetFrombitcoinCode("FTC");
            Add(new BTCPayNetwork()
            {
                bitcoinCode = nbxplorerNetwork.bitcoinCode,
                DisplayName = "Feathercoin",
                BlockExplorerLink = NetworkType == NetworkType.Mainnet ? "https://explorer.feathercoin.com/tx/{0}" : "https://explorer.feathercoin.com/tx/{0}",
                NBXplorerNetwork = nbxplorerNetwork,
                UriScheme = "feathercoin",
                DefaultRateRules = new[] 
                {
                                "FTC_X = FTC_BTC * BTC_X",
                                "FTC_BTC = bittrex(FTC_BTC)"
                },
                bitcoinImagePath = "imlegacy/feathercoin.png",
                DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(NetworkType),
                CoinType = NetworkType == NetworkType.Mainnet ? new KeyPath("8'") : new KeyPath("1'")
            });
        }
    }
}
