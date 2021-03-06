﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBXplorer;

namespace BTCPayServer
{
    public partial class BTCPayNetworkProvider
    {
        public void InitBitcore()
        {
            var nbxplorerNetwork = NBXplorerNetworkProvider.GetFrombitcoinCode("BTX");
            Add(new BTCPayNetwork()
            {
                bitcoinCode = nbxplorerNetwork.bitcoinCode,
                DisplayName = "Bitcore",
                BlockExplorerLink = NetworkType == NetworkType.Mainnet ? "https://insight.bitcore.cc/tx/{0}" : "https://insight.bitcore.cc/tx/{0}",
                NBXplorerNetwork = nbxplorerNetwork,
                UriScheme = "bitcore",
                DefaultRateRules = new[]
                {
                                "BTX_X = BTX_BTC * BTC_X",
                                "BTX_BTC = hitbtc(BTX_BTC)"
                },
                bitcoinImagePath = "imlegacy/bitcore.svg",
                LightningImagePath = "imlegacy/bitcore-lightning.svg",
                DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(NetworkType),
                CoinType = NetworkType == NetworkType.Mainnet ? new KeyPath("160'") : new KeyPath("1'")
            });
        }
    }
}
