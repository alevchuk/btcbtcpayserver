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
        public void InitUfo()
        {
            var nbxplorerNetwork = NBXplorerNetworkProvider.GetFrombitcoinCode("UFO");
            Add(new BTCPayNetwork()
            {
                bitcoinCode = nbxplorerNetwork.bitcoinCode,
                DisplayName = "Ufo",
                BlockExplorerLink = NetworkType == NetworkType.Mainnet ? "https://chainz.bitcoinid.info/ufo/tx.dws?{0}" : "https://chainz.bitcoinid.info/ufo/tx.dws?{0}",
                NBXplorerNetwork = nbxplorerNetwork,
                UriScheme = "ufo",
                DefaultRateRules = new[] 
                {
                                "UFO_X = UFO_BTC * BTC_X",
                                "UFO_BTC = coinexchange(UFO_BTC)"
                },
                bitcoinImagePath = "imlegacy/ufo.png",
                DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(NetworkType),
                CoinType = NetworkType == NetworkType.Mainnet ? new KeyPath("202'") : new KeyPath("1'")
            });
        }
    }
}
