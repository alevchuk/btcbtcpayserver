using NBitcoin;

namespace BTCPayServer
{
    public partial class BTCPayNetworkProvider
    {
        public void InitBitcoinGold()
        {
            var nbxplorerNetwork = NBXplorerNetworkProvider.GetFrombitcoinCode("BTG");
            Add(new BTCPayNetwork()
            {
                bitcoinCode = nbxplorerNetwork.bitcoinCode,
                DisplayName = "BGold",
                BlockExplorerLink = NetworkType == NetworkType.Mainnet ? "https://explorer.bitcoingold.org/insight/tx/{0}/" : "https://test-explorer.bitcoingold.org/insight/tx/{0}",
                NBXplorerNetwork = nbxplorerNetwork,
                UriScheme = "bitcoingold",
                DefaultRateRules = new[]
                {
                    "BTG_X = BTG_BTC * BTC_X",
                    "BTG_BTC = bitfinex(BTG_BTC)",
                },
                bitcoinImagePath = "imlegacy/btg.svg",
                LightningImagePath = "imlegacy/btg-lightning.svg",
                DefaultSettings = BTCPayDefaultSettings.GetDefaultSettings(NetworkType),
                CoinType = NetworkType == NetworkType.Mainnet ? new KeyPath("156'") : new KeyPath("1'")
            });
        }
    }
}
