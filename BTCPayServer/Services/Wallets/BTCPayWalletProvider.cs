﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BTCPayServer.Services.Wallets
{
    public class BTCPayWalletProvider
    {
        private ExplorerClientProvider _Client;
        BTCPayNetworkProvider _NetworkProvider;
        IOptions<MemoryCacheOptions> _Options;
        public BTCPayWalletProvider(ExplorerClientProvider client,
                                    IOptions<MemoryCacheOptions> memoryCacheOption,
                                    Data.ApplicationDbContextFactory dbContextFactory,
                                    BTCPayNetworkProvider networkProvider)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            _Client = client;
            _NetworkProvider = networkProvider;
            _Options = memoryCacheOption;

            foreach(var network in networkProvider.GetAll().OfType<BTCPayNetwork>())
            {
                var explorerClient = _Client.GetExplorerClient(network.bitcoinCode);
                if (explorerClient == null)
                    continue;
                _Wallets.Add(network.bitcoinCode.ToUpperInvariant(), new BTCPayWallet(explorerClient, new MemoryCache(_Options), network, dbContextFactory));
            }
        }

        Dictionary<string, BTCPayWallet> _Wallets = new Dictionary<string, BTCPayWallet>();

        public BTCPayWallet GetWallet(BTCPayNetworkBase network)
        {
            if (network == null)
                throw new ArgumentNullException(nameof(network));
            return GetWallet(network.bitcoinCode);
        }
        public BTCPayWallet GetWallet(string bitcoinCode)
        {
            if (bitcoinCode == null)
                throw new ArgumentNullException(nameof(bitcoinCode));
            _Wallets.TryGetValue(bitcoinCode.ToUpperInvariant(), out var result);
            return result;
        }

        public bool IsAvailable(BTCPayNetworkBase network)
        {
            return _Client.IsAvailable(network);
        }

        public IEnumerable<BTCPayWallet> GetWallets()
        {
            foreach (var w in _Wallets)
                yield return w.Value;
        }
    }
}
