using NBXplorer.Models;

namespace BTCPayServer.Events
{
    public class NewOnChainTransactionEvent
    {
        public NewTransactionEvent NewTransactionEvent { get; set; }
        public string bitcoinCode { get; set; }

        public override string ToString()
        {
            var state = NewTransactionEvent.BlockId == null ? "Unconfirmed" : "Confirmed";
            return $"{bitcoinCode}: New transaction {NewTransactionEvent.TransactionData.TransactionHash} ({state})";
        }
    }
}
