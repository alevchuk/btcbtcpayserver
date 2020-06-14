using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;

namespace BTCPayServer
{
    public partial class BTCPayNetworkProvider
    {
        public void InitGroestlcoin()
        {
            var nbxplorerNetwork = NBXplorerNetworkProvider.GetFrombitcoinCode("GRS");
            Add(new BTCPayNetwork()
            {
                bitcoinCode = nbxplorerNetwork.bitcoinCode,
                DisplayName = "Groestlcoin",
                BlockExplorerLink = NetworkType == NetworkType.Mainnet
                    ? "https://chainz.bitcoinid.info/grs/tx.dws?{0}.htm"
                    : "https://chainz.bitcoinid.info/grs-test/tx.dws?{0}.htm",
                NBXplorerNetwork = nbxplorerNetwork,
                UriScheme = "groestlcoin",
                DefaultRateRules = new[]
                {
                    "GRS_X = GRS_BTC * BTC_X",
                    "GRS_BTC = bittrex(GRS_BTC)"
                },
                bitcoinImagePath = "imlegacy/groestlcoin.png",
                LightningImagePath = "imlegacy/groestlcoin-lightning.svg",
                DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(NetworkType),
                CoinType = NetworkType == NetworkType.Mainnet ? new KeyPath("17'") : new KeyPath("1'"),
                SupportRBF = true,
                SupportPayJoin = true
            });
        }
    }
}
