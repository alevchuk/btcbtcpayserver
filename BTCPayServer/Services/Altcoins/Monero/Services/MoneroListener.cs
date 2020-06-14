using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Events;
using BTCPayServer.Services.Altcoins.Monero.Configuration;
using BTCPayServer.Services.Altcoins.Monero.Payments;
using BTCPayServer.Services.Altcoins.Monero.RPC;
using BTCPayServer.Services.Altcoins.Monero.RPC.Models;
using BTCPayServer.Payments;
using BTCPayServer.Services.Invoices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBXplorer;

namespace BTCPayServer.Services.Altcoins.Monero.Services
{
    public class MoneroListener : IHostedService
    {
        private readonly InvoiceRepository _invoiceRepository;
        private readonly EventAggregator _eventAggregator;
        private readonly MoneroRPCProvider _moneroRpcProvider;
        private readonly MoneroLikeConfiguration _MoneroLikeConfiguration;
        private readonly BTCPayNetworkProvider _networkProvider;
        private readonly ILogger<MoneroListener> _logger;
        private CompositeDisposable leases = new CompositeDisposable();
        private Queue<Func<CancellationToken, Task>> taskQueue = new Queue<Func<CancellationToken, Task>>();
        private CancellationTokenSource _Cts;

        public MoneroListener(InvoiceRepository invoiceRepository,
            EventAggregator eventAggregator,
            MoneroRPCProvider moneroRpcProvider,
            MoneroLikeConfiguration moneroLikeConfiguration,
            BTCPayNetworkProvider networkProvider,
            ILogger<MoneroListener> logger)
        {
            _invoiceRepository = invoiceRepository;
            _eventAggregator = eventAggregator;
            _moneroRpcProvider = moneroRpcProvider;
            _MoneroLikeConfiguration = moneroLikeConfiguration;
            _networkProvider = networkProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_MoneroLikeConfiguration.MoneroLikeConfigurationItems.Any())
            {
                return Task.CompletedTask;
            }
            _Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            leases.Add(_eventAggregator.Subscribe<MoneroEvent>(OnMoneroEvent));
            leases.Add(_eventAggregator.Subscribe<MoneroRPCProvider.MoneroDaemonStateChange>(e =>
            {
                if (_moneroRpcProvider.IsAvailable(e.bitcoinCode))
                {
                    _logger.LogInformation($"{e.bitcoinCode} just became available");
                    _ = UpdateAnyPendingMoneroLikePayment(e.bitcoinCode);
                }
                else
                {
                    _logger.LogInformation($"{e.bitcoinCode} just became unavailable");
                }
            }));
            _ = WorkThroughQueue(_Cts.Token);
            return Task.CompletedTask;
        }

        private async Task WorkThroughQueue(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (taskQueue.TryDequeue(out var t))
                {
                    try
                    {

                        await t.Invoke(token);
                    }
                    catch (Exception e)
                    {
                        
                        _logger.LogError($"error with queue item",e);
                    }
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                }
            }
        }

        private void OnMoneroEvent(MoneroEvent obj)
        {
            if (!_moneroRpcProvider.IsAvailable(obj.bitcoinCode))
            {
                return;
            }

            if (!string.IsNullOrEmpty(obj.BlockHash))
            {
                taskQueue.Enqueue(token => OnNewBlock(obj.bitcoinCode));
            }

            if (!string.IsNullOrEmpty(obj.TransactionHash))
            {
                taskQueue.Enqueue(token => OnTransactionUpdated(obj.bitcoinCode, obj.TransactionHash));
            }
        }

        private async Task ReceivedPayment(InvoiceEntity invoice, PaymentEntity payment)
        {
            _logger.LogInformation(
                $"Invoice {invoice.Id} received payment {payment.GetbitcoinPaymentData().GetValue()} {payment.GetbitcoinCode()} {payment.GetbitcoinPaymentData().GetPaymentId()}");
            var paymentData = (MoneroLikePaymentData)payment.GetbitcoinPaymentData();
            var paymentMethod = invoice.GetPaymentMethod(payment.Network, MoneroPaymentType.Instance);
            if (paymentMethod != null &&
                paymentMethod.GetPaymentMethodDetails() is MoneroLikeOnChainPaymentMethodDetails monero &&
                monero.GetPaymentDestination() == paymentData.GetDestination() &&
                paymentMethod.Calculate().Due > Money.Zero)
            {
                var walletClient = _moneroRpcProvider.WalletRpcClients[payment.GetbitcoinCode()];

                var address = await walletClient.SendCommandAsync<CreateAddressRequest, CreateAddressResponse>(
                    "create_address",
                    new CreateAddressRequest()
                    {
                        Label = $"btcpay invoice #{invoice.Id}", AccountIndex = monero.AccountIndex
                    });
                monero.DepositAddress = address.Address;
                monero.AddressIndex = address.AddressIndex;
                await _invoiceRepository.NewAddress(invoice.Id, monero, payment.Network);
                _eventAggregator.Publish(
                    new InvoiceNewAddressEvent(invoice.Id, address.Address, payment.Network));
                paymentMethod.SetPaymentMethodDetails(monero);
                invoice.SetPaymentMethod(paymentMethod);
            }

            _eventAggregator.Publish(
                new InvoiceEvent(invoice, 1002, InvoiceEvent.ReceivedPayment) {Payment = payment});
        }

        private async Task UpdatePaymentStates(string bitcoinCode, InvoiceEntity[] invoices)
        {
            if (!invoices.Any())
            {
                return;
            }

            var moneroWalletRpcClient = _moneroRpcProvider.WalletRpcClients[bitcoinCode];
            var network = _networkProvider.GetNetwork(bitcoinCode);


            //get all the required data in one list (invoice, its existing payments and the current payment method details)
            var expandedInvoices = invoices.Select(entity => (Invoice: entity,
                    ExistingPayments: GetAllMoneroLikePayments(entity, bitcoinCode),
                    PaymentMethodDetails: entity.GetPaymentMethod(network, MoneroPaymentType.Instance)
                        .GetPaymentMethodDetails() as MoneroLikeOnChainPaymentMethodDetails))
                .Select(tuple => (
                    tuple.Invoice,
                    tuple.PaymentMethodDetails,
                    ExistingPayments: tuple.ExistingPayments.Select(entity =>
                        (Payment: entity, PaymentData: (MoneroLikePaymentData)entity.GetbitcoinPaymentData(),
                            tuple.Invoice))
                ));

            var existingPaymentData = expandedInvoices.SelectMany(tuple => tuple.ExistingPayments);

            var accountToAddressQuery = new Dictionary<long, List<long>>();
            //create list of subaddresses to account to query the monero wallet
            foreach (var expandedInvoice in expandedInvoices)
            {
                var addressIndexList =
                    accountToAddressQuery.GetValueOrDefault(expandedInvoice.PaymentMethodDetails.AccountIndex,
                        new List<long>());

                addressIndexList.AddRange(
                    expandedInvoice.ExistingPayments.Select(tuple => tuple.PaymentData.SubaddressIndex));
                addressIndexList.Add(expandedInvoice.PaymentMethodDetails.AddressIndex);
                accountToAddressQuery.AddOrReplace(expandedInvoice.PaymentMethodDetails.AccountIndex, addressIndexList);
            }

            var tasks = accountToAddressQuery.ToDictionary(datas => datas.Key,
                datas => moneroWalletRpcClient.SendCommandAsync<GetTransfersRequest, GetTransfersResponse>(
                    "get_transfers",
                    new GetTransfersRequest()
                    {
                        AccountIndex = datas.Key, In = true, SubaddrIndices = datas.Value.Distinct().ToList()
                    }));

            await Task.WhenAll(tasks.Values);


            var transferProcessingTasks = new List<Task>();

            var updatedPaymentEntities = new BlockingCollection<(PaymentEntity Payment, InvoiceEntity invoice)>();
            foreach (var keyValuePair in tasks)
            {
                var transfers = keyValuePair.Value.Result.In;
                if (transfers == null)
                {
                    continue;
                }

                transferProcessingTasks.AddRange(transfers.Select(transfer =>
                {
                    InvoiceEntity invoice = null;
                    var existingMatch = existingPaymentData.SingleOrDefault(tuple =>
                        tuple.PaymentData.Address == transfer.Address &&
                        tuple.PaymentData.TransactionId == transfer.Txid);

                    if (existingMatch.Invoice != null)
                    {
                        invoice = existingMatch.Invoice;
                    }
                    else
                    {
                        var newMatch = expandedInvoices.SingleOrDefault(tuple =>
                            tuple.PaymentMethodDetails.GetPaymentDestination() == transfer.Address);

                        if (newMatch.Invoice == null)
                        {
                            return Task.CompletedTask;
                        }

                        invoice = newMatch.Invoice;
                    }


                    return HandlePaymentData(bitcoinCode, transfer.Address, transfer.Amount, transfer.SubaddrIndex.Major,
                        transfer.SubaddrIndex.Minor, transfer.Txid, transfer.Confirmations, transfer.Height, invoice,
                        updatedPaymentEntities);
                }));
            }

            transferProcessingTasks.Add(
                _invoiceRepository.UpdatePayments(updatedPaymentEntities.Select(tuple => tuple.Item1).ToList()));
            await Task.WhenAll(transferProcessingTasks);
            foreach (var valueTuples in updatedPaymentEntities.GroupBy(entity => entity.Item2))
            {
                if (valueTuples.Any())
                {
                    _eventAggregator.Publish(new Events.InvoiceNeedUpdateEvent(valueTuples.Key.Id));
                }
            }
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            leases.Dispose();
            _Cts?.Cancel();
            return Task.CompletedTask;
        }

        private async Task OnNewBlock(string bitcoinCode)
        {
            await  UpdateAnyPendingMoneroLikePayment(bitcoinCode);
            _eventAggregator.Publish(new NewBlockEvent() {bitcoinCode = bitcoinCode});
        }

        private async Task OnTransactionUpdated(string bitcoinCode, string transactionHash)
        {
            var paymentMethodId = new PaymentMethodId(bitcoinCode, MoneroPaymentType.Instance);
            var transfer = await _moneroRpcProvider.WalletRpcClients[bitcoinCode]
                .SendCommandAsync<GetTransferByTransactionIdRequest, GetTransferByTransactionIdResponse>(
                    "get_transfer_by_txid",
                    new GetTransferByTransactionIdRequest() {TransactionId = transactionHash});

            var paymentsToUpdate = new BlockingCollection<(PaymentEntity Payment, InvoiceEntity invoice)>();
            
            //group all destinations of the tx together and loop through the sets
            foreach (var destination in transfer.Transfers.GroupBy(destination => destination.Address))
            {
                //find the invoice corresponding to this address, else skip
                var address = destination.Key + "#" + paymentMethodId;
                var invoice = (await _invoiceRepository.GetInvoicesFromAddresses(new[] {address})).FirstOrDefault();
                if (invoice == null)
                {
                    continue;
                }

                var index = destination.First().SubaddrIndex;

                await HandlePaymentData(bitcoinCode,
                    destination.Key,
                    destination.Sum(destination1 => destination1.Amount),
                    index.Major,
                    index.Minor,
                    transfer.Transfer.Txid,
                    transfer.Transfer.Confirmations,
                    transfer.Transfer.Height
                    , invoice, paymentsToUpdate);
            }

            if (paymentsToUpdate.Any())
            {
                await _invoiceRepository.UpdatePayments(paymentsToUpdate.Select(tuple => tuple.Payment).ToList());
                foreach (var valueTuples in paymentsToUpdate.GroupBy(entity => entity.invoice))
                {
                    if (valueTuples.Any())
                    {
                        _eventAggregator.Publish(new Events.InvoiceNeedUpdateEvent(valueTuples.Key.Id));
                    }
                }
            }
        }

        private async Task HandlePaymentData(string bitcoinCode, string address, long totalAmount, long subaccountIndex,
            long subaddressIndex,
            string txId, long confirmations, long blockHeight, InvoiceEntity invoice,
            BlockingCollection<(PaymentEntity Payment, InvoiceEntity invoice)> paymentsToUpdate)
        {
            //construct the payment data
            var paymentData = new MoneroLikePaymentData()
            {
                Address = address,
                SubaccountIndex = subaccountIndex,
                SubaddressIndex = subaddressIndex,
                TransactionId = txId,
                ConfirmationCount = confirmations,
                Amount = totalAmount,
                BlockHeight = blockHeight,
                Network = _networkProvider.GetNetwork(bitcoinCode)
            };

            //check if this tx exists as a payment to this invoice already
            var alreadyExistingPaymentThatMatches = GetAllMoneroLikePayments(invoice, bitcoinCode)
                .Select(entity => (Payment: entity, PaymentData: entity.GetbitcoinPaymentData()))
                .SingleOrDefault(c => c.PaymentData.GetPaymentId() == paymentData.GetPaymentId());

            //if it doesnt, add it and assign a new monerolike address to the system if a balance is still due
            if (alreadyExistingPaymentThatMatches.Payment == null)
            {
                var payment = await _invoiceRepository.AddPayment(invoice.Id, DateTimeOffset.UtcNow,
                    paymentData, _networkProvider.GetNetwork<MoneroLikeSpecificBtcPayNetwork>(bitcoinCode), true);
                if (payment != null)
                    await ReceivedPayment(invoice, payment);
            }
            else
            {
                //else update it with the new data
                alreadyExistingPaymentThatMatches.PaymentData = paymentData;
                alreadyExistingPaymentThatMatches.Payment.SetbitcoinPaymentData(paymentData);
                paymentsToUpdate.Add((alreadyExistingPaymentThatMatches.Payment, invoice));
            }
        }

        private async Task UpdateAnyPendingMoneroLikePayment(string bitcoinCode)
        {
            var invoiceIds =await _invoiceRepository.GetPendingInvoices();
            if (!invoiceIds.Any())
            {
                return;
            }

            var invoices = await _invoiceRepository.GetInvoices(new InvoiceQuery() {InvoiceId = invoiceIds});
            invoices = invoices.Where(entity => entity.GetPaymentMethod(new PaymentMethodId(bitcoinCode, MoneroPaymentType.Instance)) != null).ToArray();
            _logger.LogInformation($"Updating pending payments for {bitcoinCode} in {string.Join(',', invoiceIds)}");
            await UpdatePaymentStates(bitcoinCode, invoices);
        }

        private IEnumerable<PaymentEntity> GetAllMoneroLikePayments(InvoiceEntity invoice, string bitcoinCode)
        {
            return invoice.GetPayments()
                .Where(p => p.GetPaymentMethodId() == new PaymentMethodId(bitcoinCode, MoneroPaymentType.Instance));
        }
    }
}
