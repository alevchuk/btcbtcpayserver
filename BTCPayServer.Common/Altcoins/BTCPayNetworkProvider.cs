using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBXplorer;

namespace BTCPayServer
{
    public partial class BTCPayNetworkProvider
    {
        Dictionary<string, BTCPayNetworkBase> _Networks = new Dictionary<string, BTCPayNetworkBase>();


        private readonly NBXplorerNetworkProvider _NBXplorerNetworkProvider;
        public NBXplorerNetworkProvider NBXplorerNetworkProvider
        {
            get
            {
                return _NBXplorerNetworkProvider;
            }
        }

        BTCPayNetworkProvider(BTCPayNetworkProvider unfiltered, string[] bitcoinCodes)
        {
            UnfilteredNetworks = unfiltered.UnfilteredNetworks ?? unfiltered;
            NetworkType = unfiltered.NetworkType;
            _NBXplorerNetworkProvider = new NBXplorerNetworkProvider(unfiltered.NetworkType);
            _Networks = new Dictionary<string, BTCPayNetworkBase>();
            bitcoinCodes = bitcoinCodes.Select(c => c.ToUpperInvariant()).ToArray();
            foreach (var network in unfiltered._Networks)
            {
                if(bitcoinCodes.Contains(network.Key))
                {
                    _Networks.Add(network.Key, network.Value);
                }
            }
        }

        public BTCPayNetworkProvider UnfilteredNetworks { get; }

        public NetworkType NetworkType { get; private set; }
        public BTCPayNetworkProvider(NetworkType networkType)
        {
            UnfilteredNetworks = this;
            _NBXplorerNetworkProvider = new NBXplorerNetworkProvider(networkType);
            NetworkType = networkType;
            InitBitcoin();
            InitLiquid();
            InitLiquidAssets();
            InitLitecoin();
            InitBitcore();
            InitDogecoin();
            InitBitcoinGold();
            InitMonacoin();
            InitDash();
            InitFeathercoin();
            InitGroestlcoin();
            InitViacoin();
            InitMonero();
            InitPolis();

            // Assume that electrum mappings are same as BTC if not specified
            foreach (var network in _Networks.Values.OfType<BTCPayNetwork>())
            {
                if(network.ElectrumMapping.Count == 0)
                {
                    network.ElectrumMapping = GetNetwork<BTCPayNetwork>("BTC").ElectrumMapping;
                    if (!network.NBitcoinNetwork.Consensus.SupportSegwit)
                    {
                        network.ElectrumMapping =
                            network.ElectrumMapping
                            .Where(kv => kv.Value == DerivationType.Legacy)
                            .ToDictionary(k => k.Key, k => k.Value);
                    }
                }
            }

            // Disabled because of https://twitter.com/bitcoinpia_NZ/status/1085084168852291586
            //InitBitcoinplus();
            //InitUfo();
        }

        /// <summary>
        /// Keep only the specified bitcoin
        /// </summary>
        /// <param name="bitcoinCodes">bitcoin to support</param>
        /// <returns></returns>
        public BTCPayNetworkProvider Filter(string[] bitcoinCodes)
        {
            return new BTCPayNetworkProvider(this, bitcoinCodes);
        }

        [Obsolete("To use only for legacy stuff")]
        public BTCPayNetwork BTC => GetNetwork<BTCPayNetwork>("BTC");

        public void Add(BTCPayNetwork network)
        {
            if (network.NBitcoinNetwork == null)
                return;
            Add(network as BTCPayNetworkBase);
        }
        public void Add(BTCPayNetworkBase network)
        {
            _Networks.Add(network.bitcoinCode.ToUpperInvariant(), network);
        }

        public IEnumerable<BTCPayNetworkBase> GetAll()
        {
            return _Networks.Values.ToArray();
        }

        public bool Support(string bitcoinCode)
        {
            return _Networks.ContainsKey(bitcoinCode.ToUpperInvariant());
        }
        public BTCPayNetworkBase GetNetwork(string bitcoinCode)
        {
            return GetNetwork<BTCPayNetworkBase>(bitcoinCode.ToUpperInvariant());
        }
        public T GetNetwork<T>(string bitcoinCode) where T: BTCPayNetworkBase
        {
            if (bitcoinCode == null)
                throw new ArgumentNullException(nameof(bitcoinCode));
            if(!_Networks.TryGetValue(bitcoinCode.ToUpperInvariant(), out BTCPayNetworkBase network))
            {
                if (bitcoinCode == "XBT")
                    return GetNetwork<T>("BTC");
            }
            return network as T;
        }
    }
}
