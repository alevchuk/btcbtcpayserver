using BTCPayServer.Payments;

namespace BTCPayServer.Services.Altcoins.Monero.Payments
{
    public class MoneroSupportedPaymentMethod : ISupportedPaymentMethod
    {

        public string bitcoinCode { get; set; }
        public long AccountIndex { get; set; }
        public PaymentMethodId PaymentId => new PaymentMethodId(bitcoinCode, MoneroPaymentType.Instance);
    }
}
