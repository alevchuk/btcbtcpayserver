﻿using NBitcoin;

namespace BTCPayServer
{
    public partial class BTCPayNetworkProvider
    {
        public void InitDash()
        {
            //not needed: NBitcoin.Altcoins.Dash.Instance.EnsureRegistered();
            var nbxplorerNetwork = NBXplorerNetworkProvider.GetFrombitcoinCode("DASH");
            Add(new BTCPayNetwork()
            {
                bitcoinCode = nbxplorerNetwork.bitcoinCode,
                DisplayName = "Dash",
                BlockExplorerLink = NetworkType == NetworkType.Mainnet
                    ? "https://insight.dash.org/insight/tx/{0}"
                    : "https://testnet-insight.dashevo.org/insight/tx/{0}",
                NBXplorerNetwork = nbxplorerNetwork,
                UriScheme = "dash",
                DefaultRateRules = new[]
                    {
                        "DASH_X = DASH_BTC * BTC_X",
                        "DASH_BTC = bittrex(DASH_BTC)"
                    },
                bitcoinImagePath = "imlegacy/dash.png",
                DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(NetworkType),
                //https://github.com/satoshilabs/slips/blob/master/slip-0044.md
                CoinType = NetworkType == NetworkType.Mainnet ? new KeyPath("5'")
                    : new KeyPath("1'")
            });
        }
    }
}
