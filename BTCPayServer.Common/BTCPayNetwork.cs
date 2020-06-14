﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NBitcoin;
using NBXplorer;
using NBXplorer.Models;
using Newtonsoft.Json;

namespace BTCPayServer
{
    public enum DerivationType
    {
        Legacy,
        SegwitP2SH,
        Segwit
    }
    public class BTCPayDefaultSettings
    {
        static BTCPayDefaultSettings()
        {
            _Settings = new Dictionary<NetworkType, BTCPayDefaultSettings>();
            foreach (var chainType in new[] { NetworkType.Mainnet, NetworkType.Testnet, NetworkType.Regtest })
            {
                var settings = new BTCPayDefaultSettings();
                _Settings.Add(chainType, settings);
                settings.DefaultDataDirectory = StandardConfiguration.DefaultDataDirectory.GetDirectory("BTCPayServer", NBXplorerDefaultSettings.GetFolderName(chainType));
                settings.DefaultConfigurationFile = Path.Combine(settings.DefaultDataDirectory, "settings.config");
                settings.DefaultPort = (chainType == NetworkType.Mainnet ? 23000 :
                                                      chainType == NetworkType.Regtest ? 23002 :
                                                      chainType == NetworkType.Testnet ? 23001 : throw new NotSupportedException(chainType.ToString()));
            }
        }

        static Dictionary<NetworkType, BTCPayDefaultSettings> _Settings;

        public static BTCPayDefaultSettings GetDefaultSettings(NetworkType chainType)
        {
            return _Settings[chainType];
        }

        public string DefaultDataDirectory { get; set; }
        public string DefaultConfigurationFile { get; set; }
        public int DefaultPort { get; set; }
    }

    public class BTCPayNetwork:BTCPayNetworkBase
    {
        public Network NBitcoinNetwork { get { return  NBXplorerNetwork?.NBitcoinNetwork; } }
        public NBXplorer.NBXplorerNetwork NBXplorerNetwork { get; set; }
        public bool SupportRBF { get; internal set; }
        public string LightningImagePath { get; set; }
        public BTCPayDefaultSettings DefaultSettings { get; set; }
        public KeyPath CoinType { get; internal set; }
        
        public Dictionary<uint, DerivationType> ElectrumMapping = new Dictionary<uint, DerivationType>();

        public virtual bool WalletSupported { get; set; } = true;
        public virtual bool ReadonlyWallet{ get; set; } = false;
        
        public int MaxTrackedConfirmation { get; internal set; } = 6;
        public string UriScheme { get; internal set; }
        public bool SupportPayJoin { get; set; } = false;
        public bool SupportLightning { get; set; } = true;

        public KeyPath GetRootKeyPath(DerivationType type)
        {
            KeyPath baseKey;
            if (!NBitcoinNetwork.Consensus.SupportSegwit)
            {
                baseKey = new KeyPath("44'");
            }
            else
            {
                switch (type)
                {
                    case DerivationType.Legacy:
                        baseKey = new KeyPath("44'");
                        break;
                    case DerivationType.SegwitP2SH:
                        baseKey = new KeyPath("49'");
                        break;
                    case DerivationType.Segwit:
                        baseKey = new KeyPath("84'");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }
            return baseKey
                .Derive(CoinType);
        }

        public KeyPath GetRootKeyPath()
        {
            return new KeyPath(NBitcoinNetwork.Consensus.SupportSegwit ? "49'" : "44'")
                .Derive(CoinType);
        }

        public override T ToObject<T>(string json)
        {
            return NBXplorerNetwork.Serializer.ToObject<T>(json);
        }

        public override string ToString<T>(T obj)
        {
            return NBXplorerNetwork.Serializer.ToString(obj);
        }
        public virtual IEnumerable<(MatchedOutput matchedOutput, OutPoint outPoint)> GetValidOutputs(NewTransactionEvent evtOutputs)
        {
            return evtOutputs.Outputs.Select(output =>
            {
                var outpoint = new OutPoint(evtOutputs.TransactionData.TransactionHash, output.Index);
                return (output, outpoint);
            });
        }

        public virtual string GenerateBIP21(string bitcoinInfoAddress, Money bitcoinInfoDue)
        {
            return $"{UriScheme}:{bitcoinInfoAddress}?amount={bitcoinInfoDue.ToString(false, true)}";
        }

        public virtual GetTransactionsResponse FilterValidTransactions(GetTransactionsResponse response)
        {
            return response;
        }
    }

    public abstract class BTCPayNetworkBase
    {
        public bool ShowSyncSummary { get; set; } = true;
        public string bitcoinCode { get; internal set; }
        public string BlockExplorerLink { get; internal set; }
        public string DisplayName { get; set; }
        public int Divisibility { get; set; } = 8;
        [Obsolete("Should not be needed")]
        public bool IsBTC
        {
            get
            {
                return bitcoinCode == "BTC";
            }
        }

        public string bitcoinImagePath { get; set; }
        public string[] DefaultRateRules { get; internal set; } = Array.Empty<string>();
        public override string ToString()
        {
            return bitcoinCode;
        }

        public virtual T ToObject<T>(string json)
        {
            return NBitcoin.JsonConverters.Serializer.ToObject<T>(json, null);
        }

        public virtual string ToString<T>(T obj)
        {
            return NBitcoin.JsonConverters.Serializer.ToString(obj, null);
        }
    }
}
