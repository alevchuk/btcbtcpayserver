using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Lightning;
using Microsoft.Extensions.Configuration;

namespace BTCPayServer.Configuration
{
    public class ExternalServices : List<ExternalService>
    {
        public void Load(string bitcoinCode, IConfiguration configuration)
        {
            Load(configuration, bitcoinCode, "lndgrpc", ExternalServiceTypes.LNDGRPC, "Invalid setting {0}, " + Environment.NewLine +
                                "lnd server: 'server=https://lnd.example.com;macaroon=abf239...;certthumbprint=2abdf302...'" + Environment.NewLine +
                                "lnd server: 'server=https://lnd.example.com;macaroonfilepath=/root/.lnd/admin.macaroon;certthumbprint=2abdf302...'" + Environment.NewLine +
                                "lnd server: 'server=https://lnd.example.com;macaroondirectorypath=/root/.lnd;certthumbprint=2abdf302...'" + Environment.NewLine +
                                "Error: {1}",
                                "LND (gRPC server)");
            Load(configuration, bitcoinCode, "lndrest", ExternalServiceTypes.LNDRest, "Invalid setting {0}, " + Environment.NewLine +
                                "lnd server: 'server=https://lnd.example.com;macaroon=abf239...;certthumbprint=2abdf302...'" + Environment.NewLine +
                                "lnd server: 'server=https://lnd.example.com;macaroonfilepath=/root/.lnd/admin.macaroon;certthumbprint=2abdf302...'" + Environment.NewLine +
                                "lnd server: 'server=https://lnd.example.com;macaroondirectorypath=/root/.lnd;certthumbprint=2abdf302...'" + Environment.NewLine +
                                "Error: {1}",
                                "LND (REST server)");
            Load(configuration, bitcoinCode, "lndseedbackup", ExternalServiceTypes.LNDSeedBackup, "Invalid setting {0}, " + Environment.NewLine +
                                "lnd seed backup: /etc/merchant_lnd/data/chain/bitcoin/regtest/walletunlock.json'" + Environment.NewLine +
                                "Error: {1}",
                                "LND Seed Backup");
            Load(configuration, bitcoinCode, "spark", ExternalServiceTypes.Spark, "Invalid setting {0}, " + Environment.NewLine +
                                $"Valid example: 'server=https://btcpay.example.com/spark/btc/;cookiefile=/etc/clightning_bitcoin_spark/.cookie'" + Environment.NewLine +
                                "Error: {1}",
                                "C-Lightning (Spark server)");
            Load(configuration, bitcoinCode, "rtl", ExternalServiceTypes.RTL, "Invalid setting {0}, " + Environment.NewLine +
                                $"Valid example: 'server=https://btcpay.example.com/rtl/btc/;cookiefile=/etc/clightning_bitcoin_rtl/.cookie'" + Environment.NewLine +
                                "Error: {1}",
                                "Ride the Lightning server");
            Load(configuration, bitcoinCode, "thunderhub", ExternalServiceTypes.ThunderHub, "Invalid setting {0}, " + Environment.NewLine +
                                $"Valid example: 'server=https://btcpay.example.com/thub/;cookiefile=/etc/clightning_bitcoin_rtl/.cookie'" + Environment.NewLine +
                                "Error: {1}",
                                "ThunderHub");
            Load(configuration, bitcoinCode, "clightningrest", ExternalServiceTypes.CLightningRest, "Invalid setting {0}, " + Environment.NewLine +
                                $"Valid example: 'server=https://btcpay.example.com/clightning-rest/btc/;cookiefile=/etc/clightning_bitcoin_rtl/.cookie'" + Environment.NewLine +
                                "Error: {1}",
                                "C-Lightning REST");
            Load(configuration, bitcoinCode, "charge", ExternalServiceTypes.Charge, "Invalid setting {0}, " + Environment.NewLine +
                                $"lightning charge server: 'type=charge;server=https://charge.example.com;api-token=2abdf302...'" + Environment.NewLine +
                                $"lightning charge server: 'type=charge;server=https://charge.example.com;cookiefilepath=/root/.charge/.cookie'" + Environment.NewLine +
                                "Error: {1}",
                                "C-Lightning (Charge server)");
            
        }

        public void LoadNonbitcoinServices(IConfiguration configuration)
        {
            Load(configuration, null, "configurator", ExternalServiceTypes.Configurator, "Invalid setting {0}, " + Environment.NewLine +
                                                                                   $"configurator: 'cookiefilepathfile=/etc/configurator/cookie'" + Environment.NewLine +
                                                                                   "Error: {1}",
                "Configurator");
        }

        void Load(IConfiguration configuration, string bitcoinCode, string serviceName, ExternalServiceTypes type, string errorMessage, string displayName)
        {
            var setting = $"{(!string.IsNullOrEmpty(bitcoinCode)? $"{bitcoinCode}.": string.Empty)}external.{serviceName}";
            var connStr = configuration.GetOrDefault<string>(setting, string.Empty);
            if (connStr.Length != 0)
            {
                ExternalConnectionString serviceConnection;
                if (type == ExternalServiceTypes.LNDSeedBackup)
                {
                    // just using CookieFilePath to hold variable instead of refactoring whole holder class to better conform
                    serviceConnection = new ExternalConnectionString { CookieFilePath = connStr };
                }
                else if (!ExternalConnectionString.TryParse(connStr, out serviceConnection, out var error))
                {
                    throw new ConfigException(string.Format(CultureInfo.InvariantCulture, errorMessage, setting, error));
                }
                this.Add(new ExternalService() { Type = type, ConnectionString = serviceConnection, bitcoinCode = bitcoinCode, DisplayName = displayName, ServiceName = serviceName });
            }
        }

        public ExternalService GetService(string serviceName, string bitcoinCode)
        {
            return this.FirstOrDefault(o =>
                (bitcoinCode == null && o.bitcoinCode == null) ||
                (o.bitcoinCode != null && o.bitcoinCode.Equals(bitcoinCode, StringComparison.OrdinalIgnoreCase))
                &&
                o.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class ExternalService
    {
        public string DisplayName { get; set; }
        public ExternalServiceTypes Type { get; set; }
        public ExternalConnectionString ConnectionString { get; set; }
        public string bitcoinCode { get; set; }
        public string ServiceName { get; set; }
    }

    public enum ExternalServiceTypes
    {
        LNDRest,
        LNDGRPC,
        LNDSeedBackup,
        Spark,
        RTL,
        ThunderHub,
        Charge,
        P2P,
        RPC,
        Configurator,
        CLightningRest
    }
}
