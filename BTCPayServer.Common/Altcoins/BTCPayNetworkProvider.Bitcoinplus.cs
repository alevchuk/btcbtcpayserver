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
        public void InitBitcoinplus()
        {
            var nbxplorerNetwork = NBXplorerNetworkProvider.GetFrombitcoinCode("XBC");
            Add(new BTCPayNetwork()
            {
                bitcoinCode = nbxplorerNetwork.bitcoinCode,
                DisplayName = "Bitcoinplus",
                BlockExplorerLink = NetworkType == NetworkType.Mainnet ? "https://chainz.bitcoinid.info/xbc/tx.dws?{0}" : "https://chainz.bitcoinid.info/xbc/tx.dws?{0}",
                NBXplorerNetwork = nbxplorerNetwork,
                UriScheme = "bitcoinplus",
                DefaultRateRules = new[]
                {
                                "XBC_X = XBC_BTC * BTC_X",
                                "XBC_BTC = bitcoinpia(XBC_BTC)"
                },
                bitcoinImagePath = "imlegacy/bitcoinplus.png",
                DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(NetworkType),
                CoinType = NetworkType == NetworkType.Mainnet ? new KeyPath("65'") : new KeyPath("1'")
            });
        }
    }
}
