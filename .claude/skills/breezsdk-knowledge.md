---
name: breezsdk-knowledge
description: Comprehensive BreezSDK Liquid C# reference for agent consultation
globs:
  - "**/*.cs"
  - "**/*.razor"
---

# BreezSDK Liquid C# Reference

This knowledge file provides comprehensive C# patterns and code examples for BreezSDK Liquid integration. All BreezSDK expert agents reference this file.

---

## Quick Reference

| Item | Value |
|------|-------|
| **NuGet Package** | `Breez.Sdk.Liquid` |
| **Installation** | `dotnet add package Breez.Sdk.Liquid` |
| **API Key** | Required - obtain from Breez |
| **Networks** | `LiquidNetwork.Mainnet`, `LiquidNetwork.Testnet` |
| **Key Pattern** | Self-custodial (user holds keys) |
| **Underlying** | Liquid sidechain with Lightning via submarine swaps |

### Default Supported Assets

| Name | Ticker | Asset ID | Precision |
|------|--------|----------|-----------|
| Bitcoin | BTC | `6f0279e9ed041c3d710a9f57d0c02928416460c4b722ae3457a11eec381c526d` | 8 |
| Tether USD | USDt | `ce091c998b83c78bb71a632313ba3760f1763d9cfcffae02258ffa9865a37bd2` | 8 |

---

## Connection & Configuration

### Default Configuration

```csharp
using Breez.Sdk.Liquid;

// Create default configuration
var config = BreezSdkLiquidMethods.DefaultConfig(
    LiquidNetwork.Mainnet,  // or LiquidNetwork.Testnet
    "<your-Breez-API-key>"
) with {
    workingDir = "/path/to/existing/directory"
};
```

### Connect with Mnemonic

```csharp
try
{
    var mnemonic = "<your-12-or-24-word-seed>";
    var connectRequest = new ConnectRequest(config, mnemonic);
    BindingLiquidSdk sdk = BreezSdkLiquidMethods.Connect(connectRequest);
}
catch (Exception e)
{
    Console.WriteLine($"Connection failed: {e.Message}");
}
```

### Connect with External Signer

```csharp
try
{
    var connectRequest = new ConnectWithSignerRequest(config);
    BindingLiquidSdk sdk = BreezSdkLiquidMethods.ConnectWithSigner(connectRequest, signer);
}
catch (Exception e)
{
    Console.WriteLine($"Connection with signer failed: {e.Message}");
}
```

### Disconnect (Cleanup)

```csharp
try
{
    sdk.Disconnect();
}
catch (Exception e)
{
    Console.WriteLine($"Disconnect failed: {e.Message}");
}
```

**Key Points**:
- Working directory must exist before connection
- Store mnemonic securely; it's the user's private key
- Always call `Disconnect()` when done (cleanup)
- Set logger before connecting for troubleshooting

---

## Event Handling

### Implement EventListener Interface

```csharp
public class SdkListener : EventListener
{
    public void OnEvent(SdkEvent e)
    {
        Console.WriteLine($"Received event: {e}");

        // Handle specific event types
        switch (e)
        {
            case SdkEvent.PaymentSucceeded paymentEvent:
                Console.WriteLine($"Payment succeeded: {paymentEvent.Details}");
                break;
            case SdkEvent.PaymentFailed paymentEvent:
                Console.WriteLine($"Payment failed: {paymentEvent.Details}");
                break;
            case SdkEvent.PaymentPending paymentEvent:
                Console.WriteLine($"Payment pending: {paymentEvent.Details}");
                break;
            case SdkEvent.PaymentRefunded paymentEvent:
                Console.WriteLine($"Payment refunded: {paymentEvent.Details}");
                break;
            case SdkEvent.PaymentWaitingConfirmation paymentEvent:
                Console.WriteLine($"Waiting confirmation: {paymentEvent.Details}");
                break;
            case SdkEvent.Synced:
                Console.WriteLine("SDK synced with network");
                break;
        }
    }
}
```

### Add and Remove Listeners

```csharp
// Add listener - returns listener ID
var listener = new SdkListener();
string listenerId = sdk.AddEventListener(listener);

// Store listenerId for later removal

// Remove listener (CRITICAL for cleanup)
sdk.RemoveEventListener(listenerId);
```

**Key Points**:
- Always store the listener ID returned by `AddEventListener()`
- Always call `RemoveEventListener()` to prevent memory leaks
- Handle events asynchronously to avoid blocking SDK operations
- Events provide real-time payment status updates

---

## Logging

### Implement Logger Interface

```csharp
public class SdkLogger : Logger
{
    public void Log(LogEntry l)
    {
        Console.WriteLine($"[{l.level}]: {l.line}");
    }
}
```

### Set Logger Before Connection

```csharp
// MUST be called before Connect()
BreezSdkLiquidMethods.SetLogger(new SdkLogger());

// Now connect
var sdk = BreezSdkLiquidMethods.Connect(connectRequest);
```

**Key Points**:
- Set logger BEFORE calling `Connect()`
- Use DEBUG level in production for troubleshooting support
- Log entries contain `level` and `line` properties
- Logging is essential for production debugging

---

## Wallet State

### Get Wallet Information

```csharp
try
{
    GetInfoResponse? info = sdk.GetInfo();
    if (info != null)
    {
        // Balances (in satoshis)
        Console.WriteLine($"Balance (sat): {info.balanceSat}");
        Console.WriteLine($"Pending send (sat): {info.pendingSendSat}");
        Console.WriteLine($"Pending receive (sat): {info.pendingReceiveSat}");

        // Fingerprint for identification
        Console.WriteLine($"Fingerprint: {info.fingerprint}");

        // Public key
        Console.WriteLine($"Pubkey: {info.pubkey}");
    }
}
catch (Exception e)
{
    Console.WriteLine($"Failed to get info: {e.Message}");
}
```

**Key Points**:
- `balanceSat` - confirmed available balance
- `pendingSendSat` - outgoing payments not yet confirmed
- `pendingReceiveSat` - incoming payments not yet confirmed
- Always check for null return values

---

## Payment Operations - Receiving

### CRITICAL: Check Limits First

```csharp
try
{
    // Always fetch limits before any payment operation
    LightningPaymentLimitsResponse limits = sdk.FetchLightningLimits();

    Console.WriteLine($"Receive min: {limits.receive.minSat} sat");
    Console.WriteLine($"Receive max: {limits.receive.maxSat} sat");
    Console.WriteLine($"Send min: {limits.send.minSat} sat");
    Console.WriteLine($"Send max: {limits.send.maxSat} sat");
}
catch (Exception e)
{
    Console.WriteLine($"Failed to fetch limits: {e.Message}");
}
```

### Receive Lightning Payment (BOLT11)

```csharp
try
{
    // Step 1: Prepare
    var prepareRequest = new PrepareReceiveRequest(
        paymentMethod: PaymentMethod.Lightning,
        payerAmountSat: 5000UL  // Amount in satoshis
    );
    PrepareReceiveResponse prepareResponse = sdk.PrepareReceivePayment(prepareRequest);

    // Display fees to user
    Console.WriteLine($"Payer will send: {prepareResponse.payerAmountSat} sat");
    Console.WriteLine($"You will receive: {prepareResponse.paymentMethod.receiverAmountSat} sat");
    Console.WriteLine($"Fee: {prepareResponse.feesSat} sat");

    // Step 2: Execute (after user confirms)
    var receiveRequest = new ReceivePaymentRequest(prepareResponse);
    ReceivePaymentResponse response = sdk.ReceivePayment(receiveRequest);

    Console.WriteLine($"Invoice: {response.destination}");
}
catch (Exception e)
{
    Console.WriteLine($"Failed to create invoice: {e.Message}");
}
```

### Receive BOLT12 Offer (Reusable, Offline-Capable)

```csharp
try
{
    // Step 1: Prepare
    var prepareRequest = new PrepareReceiveRequest(
        paymentMethod: PaymentMethod.Bolt12Offer
        // Amount optional for BOLT12 offers
    );
    PrepareReceiveResponse prepareResponse = sdk.PrepareReceivePayment(prepareRequest);

    // Step 2: Execute
    var receiveRequest = new ReceivePaymentRequest(prepareResponse);
    ReceivePaymentResponse response = sdk.ReceivePayment(receiveRequest);

    Console.WriteLine($"BOLT12 Offer: {response.destination}");
}
catch (Exception e)
{
    Console.WriteLine($"Failed to create BOLT12 offer: {e.Message}");
}
```

### Receive Bitcoin On-Chain

```csharp
try
{
    // Check on-chain limits first
    OnchainPaymentLimitsResponse limits = sdk.FetchOnchainLimits();
    Console.WriteLine($"On-chain receive min: {limits.receive.minSat} sat");
    Console.WriteLine($"On-chain receive max: {limits.receive.maxSat} sat");

    // Step 1: Prepare
    var prepareRequest = new PrepareReceiveRequest(
        paymentMethod: PaymentMethod.BitcoinAddress,
        payerAmountSat: 100000UL  // Amount in satoshis
    );
    PrepareReceiveResponse prepareResponse = sdk.PrepareReceivePayment(prepareRequest);

    // Show fees (requires user acceptance for Bitcoin)
    Console.WriteLine($"Swap fees: {prepareResponse.feesSat} sat");

    // Step 2: Execute
    var receiveRequest = new ReceivePaymentRequest(prepareResponse);
    ReceivePaymentResponse response = sdk.ReceivePayment(receiveRequest);

    Console.WriteLine($"Bitcoin address: {response.destination}");
}
catch (Exception e)
{
    Console.WriteLine($"Failed to create Bitcoin address: {e.Message}");
}
```

### Receive Liquid (Direct)

```csharp
try
{
    // Step 1: Prepare
    var prepareRequest = new PrepareReceiveRequest(
        paymentMethod: PaymentMethod.LiquidAddress
    );
    PrepareReceiveResponse prepareResponse = sdk.PrepareReceivePayment(prepareRequest);

    // Step 2: Execute
    var receiveRequest = new ReceivePaymentRequest(prepareResponse);
    ReceivePaymentResponse response = sdk.ReceivePayment(receiveRequest);

    Console.WriteLine($"Liquid address: {response.destination}");
}
catch (Exception e)
{
    Console.WriteLine($"Failed to create Liquid address: {e.Message}");
}
```

**Key Points**:
- Always use two-step pattern: `PrepareReceivePayment()` → `ReceivePayment()`
- Always call `FetchLightningLimits()` or `FetchOnchainLimits()` first
- Display fees to user before executing payment
- BOLT12 offers are reusable and work offline
- Bitcoin addresses require swap fee acceptance

---

## Payment Operations - Sending

### Send Lightning Payment

```csharp
try
{
    // Check limits first
    LightningPaymentLimitsResponse limits = sdk.FetchLightningLimits();

    // Step 1: Prepare
    string bolt11Invoice = "lnbc...";
    var prepareRequest = new PrepareSendRequest(destination: bolt11Invoice);
    PrepareSendResponse prepareResponse = sdk.PrepareSendPayment(prepareRequest);

    // Display fees to user
    Console.WriteLine($"Amount: {prepareResponse.destination.amountSat} sat");
    Console.WriteLine($"Fee: {prepareResponse.feesSat} sat");

    // Step 2: Execute (after user confirms)
    var sendRequest = new SendPaymentRequest(prepareResponse);
    SendPaymentResponse response = sdk.SendPayment(sendRequest);

    Console.WriteLine($"Payment sent: {response.payment}");
}
catch (Exception e)
{
    Console.WriteLine($"Failed to send payment: {e.Message}");
}
```

### Send with Amount Override

```csharp
try
{
    // For zero-amount invoices or to override amount
    var prepareRequest = new PrepareSendRequest(
        destination: bolt11Invoice,
        amountSat: 5000UL  // Specify amount
    );
    PrepareSendResponse prepareResponse = sdk.PrepareSendPayment(prepareRequest);

    var sendRequest = new SendPaymentRequest(prepareResponse);
    SendPaymentResponse response = sdk.SendPayment(sendRequest);
}
catch (Exception e)
{
    Console.WriteLine($"Failed to send payment: {e.Message}");
}
```

### Drain All Funds

```csharp
try
{
    var prepareRequest = new PrepareSendRequest(
        destination: destinationAddress,
        amountSat: null  // null = drain all funds
    );
    PrepareSendResponse prepareResponse = sdk.PrepareSendPayment(prepareRequest);

    // Show total amount being drained
    Console.WriteLine($"Draining: {prepareResponse.destination.amountSat} sat");
    Console.WriteLine($"Fee: {prepareResponse.feesSat} sat");

    var sendRequest = new SendPaymentRequest(prepareResponse);
    SendPaymentResponse response = sdk.SendPayment(sendRequest);
}
catch (Exception e)
{
    Console.WriteLine($"Failed to drain funds: {e.Message}");
}
```

**Key Points**:
- Always use two-step pattern: `PrepareSendPayment()` → `SendPayment()`
- Display fees before user confirmation
- Use `null` amount to drain all funds
- Parse destination first if type is unknown (see LNURL Operations)

---

## Payment Operations - On-Chain

### Send Bitcoin On-Chain

```csharp
try
{
    // Check limits first
    OnchainPaymentLimitsResponse limits = sdk.FetchOnchainLimits();

    // Step 1: Prepare
    string btcAddress = "bc1q...";
    var prepareRequest = new PreparePayOnchainRequest(
        amount: new PayOnchainAmount.Receiver(amountSat: 50000UL),
        feeRateSatPerVbyte: null  // Use default fee rate
    );
    PreparePayOnchainResponse prepareResponse = sdk.PreparePayOnchain(prepareRequest);

    // Display fees
    Console.WriteLine($"Claim fee: {prepareResponse.claimFeesSat} sat");
    Console.WriteLine($"Total fees: {prepareResponse.totalFeesSat} sat");

    // Step 2: Execute
    var payRequest = new PayOnchainRequest(
        address: btcAddress,
        prepareResponse: prepareResponse
    );
    SendPaymentResponse response = sdk.PayOnchain(payRequest);
}
catch (Exception e)
{
    Console.WriteLine($"Failed to pay on-chain: {e.Message}");
}
```

### Drain to On-Chain Address

```csharp
try
{
    var prepareRequest = new PreparePayOnchainRequest(
        amount: PayOnchainAmount.Drain,  // Drain all funds
        feeRateSatPerVbyte: 10UL  // Custom fee rate
    );
    PreparePayOnchainResponse prepareResponse = sdk.PreparePayOnchain(prepareRequest);

    var payRequest = new PayOnchainRequest(
        address: btcAddress,
        prepareResponse: prepareResponse
    );
    SendPaymentResponse response = sdk.PayOnchain(payRequest);
}
catch (Exception e)
{
    Console.WriteLine($"Failed to drain to on-chain: {e.Message}");
}
```

**Key Points**:
- Use `PreparePayOnchain()` → `PayOnchain()` pattern
- Always check `FetchOnchainLimits()` first
- Custom fee rate is optional; SDK provides default
- `PayOnchainAmount.Drain` sends all available funds

---

## Payment Listing & Retrieval

### List All Payments

```csharp
try
{
    var listRequest = new ListPaymentsRequest();
    List<Payment>? payments = sdk.ListPayments(listRequest);

    if (payments != null)
    {
        foreach (var payment in payments)
        {
            Console.WriteLine($"ID: {payment.id}");
            Console.WriteLine($"Type: {payment.paymentType}");
            Console.WriteLine($"Status: {payment.status}");
            Console.WriteLine($"Amount: {payment.amountSat} sat");
            Console.WriteLine("---");
        }
    }
}
catch (Exception e)
{
    Console.WriteLine($"Failed to list payments: {e.Message}");
}
```

### Filter Payments

```csharp
try
{
    var listRequest = new ListPaymentsRequest(
        filters: new List<PaymentType> { PaymentType.Receive },
        fromTimestamp: DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds(),
        toTimestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        offset: 0,
        limit: 10
    );
    List<Payment>? payments = sdk.ListPayments(listRequest);
}
catch (Exception e)
{
    Console.WriteLine($"Failed to list payments: {e.Message}");
}
```

### Get Single Payment

```csharp
try
{
    // By payment hash
    var request = new GetPaymentRequest.Lightning(paymentHash: "abc123...");
    Payment? payment = sdk.GetPayment(request);

    // Or by swap ID (for on-chain swaps)
    var swapRequest = new GetPaymentRequest.SwapId(swapId: "swap123...");
    Payment? swapPayment = sdk.GetPayment(swapRequest);
}
catch (Exception e)
{
    Console.WriteLine($"Failed to get payment: {e.Message}");
}
```

**Key Points**:
- Filter by type: `PaymentType.Receive`, `PaymentType.Send`
- Use timestamps for date range queries
- Pagination via `offset` and `limit`
- Get individual payment by hash or swap ID

---

## Refunds & Recovery

### List Refundable Swaps

```csharp
try
{
    List<RefundableSwap>? refundables = sdk.ListRefundables();

    if (refundables != null && refundables.Count > 0)
    {
        foreach (var swap in refundables)
        {
            Console.WriteLine($"Swap ID: {swap.swapAddress}");
            Console.WriteLine($"Amount: {swap.amountSat} sat");
            Console.WriteLine($"Last refund TX ID: {swap.lastRefundTxId}");
        }
    }
}
catch (Exception e)
{
    Console.WriteLine($"Failed to list refundables: {e.Message}");
}
```

### Execute Refund

```csharp
try
{
    // Get recommended fees
    ulong? recommendedFees = sdk.RecommendedFees()?.fastestFee;

    var refundRequest = new RefundRequest(
        swapAddress: refundableSwap.swapAddress,
        refundAddress: userBtcAddress,  // User's BTC address
        feeRateSatPerVbyte: recommendedFees ?? 1UL
    );
    RefundResponse response = sdk.Refund(refundRequest);

    Console.WriteLine($"Refund TX: {response.refundTxId}");
}
catch (Exception e)
{
    Console.WriteLine($"Refund failed: {e.Message}");
}
```

### Rescan On-Chain Swaps

```csharp
try
{
    // Useful if swaps are missing or stuck
    sdk.RescanOnchainSwaps();
}
catch (Exception e)
{
    Console.WriteLine($"Rescan failed: {e.Message}");
}
```

**Key Points**:
- Check `ListRefundables()` regularly for stuck swaps
- Allow users to increase fee rate for refunds
- `RescanOnchainSwaps()` can recover missing swap state
- Refunds go to a user-provided Bitcoin address

---

## LNURL Operations

### Parse Input Type

```csharp
try
{
    string input = "lnurl1...";  // Or Lightning address, invoice, etc.
    InputType inputType = sdk.Parse(input);

    switch (inputType)
    {
        case InputType.Bolt11 bolt11:
            Console.WriteLine($"BOLT11 Invoice: {bolt11.invoice}");
            break;
        case InputType.Bolt12Offer offer:
            Console.WriteLine($"BOLT12 Offer: {offer.offer}");
            break;
        case InputType.BitcoinAddress btc:
            Console.WriteLine($"Bitcoin address: {btc.address}");
            break;
        case InputType.LiquidAddress liquid:
            Console.WriteLine($"Liquid address: {liquid.address}");
            break;
        case InputType.LnUrlPay lnurlPay:
            Console.WriteLine($"LNURL-Pay: {lnurlPay.data}");
            break;
        case InputType.LnUrlWithdraw lnurlWithdraw:
            Console.WriteLine($"LNURL-Withdraw: {lnurlWithdraw.data}");
            break;
        case InputType.LnUrlAuth lnurlAuth:
            Console.WriteLine($"LNURL-Auth: {lnurlAuth.data}");
            break;
        case InputType.LnUrlError error:
            Console.WriteLine($"LNURL Error: {error.data}");
            break;
    }
}
catch (Exception e)
{
    Console.WriteLine($"Parse failed: {e.Message}");
}
```

### LNURL-Pay

```csharp
try
{
    // After parsing returns LnUrlPay
    var lnurlPayData = (inputType as InputType.LnUrlPay)!.data;

    // Step 1: Prepare
    var prepareRequest = new PrepareLnUrlPayRequest(
        data: lnurlPayData,
        amountMsat: 5000000UL,  // Amount in millisatoshis
        comment: "Payment for service",
        validateSuccessActionUrl: true
    );
    PrepareLnUrlPayResponse prepareResponse = sdk.PrepareLnurlPay(prepareRequest);

    // Display fees
    Console.WriteLine($"Fee: {prepareResponse.feesSat} sat");

    // Step 2: Execute
    var payRequest = new LnUrlPayRequest(prepareResponse);
    LnUrlPayResult result = sdk.LnurlPay(payRequest);

    // Handle result
    switch (result)
    {
        case LnUrlPayResult.EndpointSuccess success:
            Console.WriteLine($"Success: {success.data}");
            break;
        case LnUrlPayResult.EndpointError error:
            Console.WriteLine($"Error: {error.data}");
            break;
        case LnUrlPayResult.PayError payError:
            Console.WriteLine($"Payment error: {payError.data}");
            break;
    }
}
catch (Exception e)
{
    Console.WriteLine($"LNURL-Pay failed: {e.Message}");
}
```

### LNURL-Withdraw

```csharp
try
{
    // After parsing returns LnUrlWithdraw
    var lnurlWithdrawData = (inputType as InputType.LnUrlWithdraw)!.data;

    var withdrawRequest = new LnUrlWithdrawRequest(
        data: lnurlWithdrawData,
        amountMsat: 3000000UL,  // Amount in millisatoshis
        description: "Withdrawal"
    );
    LnUrlWithdrawResult result = sdk.LnurlWithdraw(withdrawRequest);

    switch (result)
    {
        case LnUrlWithdrawResult.Ok ok:
            Console.WriteLine($"Withdrawal initiated: {ok.data}");
            break;
        case LnUrlWithdrawResult.Timeout timeout:
            Console.WriteLine($"Timeout: {timeout.data}");
            break;
        case LnUrlWithdrawResult.ErrorStatus error:
            Console.WriteLine($"Error: {error.data}");
            break;
    }
}
catch (Exception e)
{
    Console.WriteLine($"LNURL-Withdraw failed: {e.Message}");
}
```

### LNURL-Auth

```csharp
try
{
    // After parsing returns LnUrlAuth
    var lnurlAuthData = (inputType as InputType.LnUrlAuth)!.data;

    var authRequest = new LnUrlAuthRequest(data: lnurlAuthData);
    LnUrlCallbackStatus result = sdk.LnurlAuth(authRequest);

    switch (result)
    {
        case LnUrlCallbackStatus.Ok:
            Console.WriteLine("Authentication successful");
            break;
        case LnUrlCallbackStatus.ErrorStatus error:
            Console.WriteLine($"Auth error: {error.data}");
            break;
    }
}
catch (Exception e)
{
    Console.WriteLine($"LNURL-Auth failed: {e.Message}");
}
```

**Key Points**:
- Always use `sdk.Parse()` to determine input type
- LNURL-Pay uses millisatoshis (msat), not satoshis
- Two-step pattern for LNURL-Pay: `PrepareLnurlPay()` → `LnurlPay()`
- Lightning addresses resolve to LNURL-Pay

---

## Multi-Asset Support

### Configure Custom Assets

```csharp
var config = BreezSdkLiquidMethods.DefaultConfig(
    LiquidNetwork.Mainnet,
    apiKey
) with {
    assetMetadata = new List<AssetMetadata> {
        new AssetMetadata(
            assetId: "custom-asset-id-here",
            name: "PEGx EUR",
            ticker: "EURx",
            precision: 8,
            fiatId: "EUR"  // For fiat rate conversion
        )
    }
};
```

### Send Specific Asset

```csharp
try
{
    var prepareRequest = new PrepareSendRequest(
        destination: invoice,
        amountSat: 5000UL
    ) with {
        assetId = "ce091c998b83c78bb71a632313ba3760f1763d9cfcffae02258ffa9865a37bd2"  // USDt
    };

    PrepareSendResponse prepareResponse = sdk.PrepareSendPayment(prepareRequest);
    var sendRequest = new SendPaymentRequest(prepareResponse);
    SendPaymentResponse response = sdk.SendPayment(sendRequest);
}
catch (Exception e)
{
    Console.WriteLine($"Asset payment failed: {e.Message}");
}
```

### Receive Specific Asset

```csharp
try
{
    var prepareRequest = new PrepareReceiveRequest(
        paymentMethod: PaymentMethod.Lightning,
        payerAmountSat: 5000UL
    ) with {
        assetId = "ce091c998b83c78bb71a632313ba3760f1763d9cfcffae02258ffa9865a37bd2"  // USDt
    };

    PrepareReceiveResponse prepareResponse = sdk.PrepareReceivePayment(prepareRequest);
    var receiveRequest = new ReceivePaymentRequest(prepareResponse);
    ReceivePaymentResponse response = sdk.ReceivePayment(receiveRequest);
}
catch (Exception e)
{
    Console.WriteLine($"Asset receive failed: {e.Message}");
}
```

### Asset Exchange (Self-Payment Swap)

```csharp
try
{
    // Exchange BTC to USDt via self-payment
    // Create receive for USDt
    var receiveRequest = new PrepareReceiveRequest(
        paymentMethod: PaymentMethod.Lightning
    ) with {
        assetId = "ce091c998b83c78bb71a632313ba3760f1763d9cfcffae02258ffa9865a37bd2"  // USDt
    };
    PrepareReceiveResponse receiveResponse = sdk.PrepareReceivePayment(receiveRequest);
    var receive = sdk.ReceivePayment(new ReceivePaymentRequest(receiveResponse));

    // Send BTC to the USDt invoice
    var sendRequest = new PrepareSendRequest(destination: receive.destination);
    PrepareSendResponse sendResponse = sdk.PrepareSendPayment(sendRequest);
    sdk.SendPayment(new SendPaymentRequest(sendResponse));
}
catch (Exception e)
{
    Console.WriteLine($"Asset exchange failed: {e.Message}");
}
```

**Key Points**:
- Default assets: BTC and USDt
- Custom assets configured in `DefaultConfig`
- Specify `assetId` in prepare requests
- Asset exchange via self-payment (receive one asset, pay from another)

---

## Fiat Currencies

### List Supported Fiat Currencies

```csharp
try
{
    List<FiatCurrency>? currencies = sdk.ListFiatCurrencies();

    if (currencies != null)
    {
        foreach (var currency in currencies)
        {
            Console.WriteLine($"{currency.id}: {currency.info.name} ({currency.info.symbol})");
        }
    }
}
catch (Exception e)
{
    Console.WriteLine($"Failed to list currencies: {e.Message}");
}
```

### Fetch Fiat Exchange Rates

```csharp
try
{
    var fiatIds = new List<string> { "USD", "EUR", "GBP" };
    List<Rate>? rates = sdk.FetchFiatRates(fiatIds);

    if (rates != null)
    {
        foreach (var rate in rates)
        {
            Console.WriteLine($"{rate.coin}: {rate.value} per unit");
        }
    }
}
catch (Exception e)
{
    Console.WriteLine($"Failed to fetch rates: {e.Message}");
}
```

**Key Points**:
- Fiat IDs are ISO 4217 codes (USD, EUR, GBP, etc.)
- Rates are returned per unit of cryptocurrency
- Use for displaying fiat equivalents in UI

---

## Message Signing

### Sign a Message

```csharp
try
{
    var signRequest = new SignMessageRequest(message: "Hello, Lightning!");
    SignMessageResponse response = sdk.SignMessage(signRequest);

    Console.WriteLine($"Signature: {response.signature}");
}
catch (Exception e)
{
    Console.WriteLine($"Sign failed: {e.Message}");
}
```

### Verify a Message Signature

```csharp
try
{
    var checkRequest = new CheckMessageRequest(
        message: "Hello, Lightning!",
        pubkey: senderPubkey,
        signature: signatureToVerify
    );
    CheckMessageResponse response = sdk.CheckMessage(checkRequest);

    Console.WriteLine($"Signature valid: {response.isValid}");
}
catch (Exception e)
{
    Console.WriteLine($"Check failed: {e.Message}");
}
```

**Key Points**:
- Sign messages with wallet private key
- Verify signatures from other nodes
- Useful for proving ownership

---

## UX Guidelines Summary

### Core Principles

1. **Simplicity over choice** - Don't make users pick protocols/rails
2. **Transparency without jargon** - Show fees/limits in plain language
3. **Progressive disclosure** - Advanced details tucked away
4. **Lightning priority** - Present Lightning as primary option

### Receive Payment UX

- **Default QR**: Show LNURL-Pay QR code
- **Lightning Address**: Display human-readable `user@domain` address
- **Primary Actions**: Copy (address) and Share (LNURL string)
- **Limits**: Display min/max amounts before payment
- **Fees**: Show fees prominently

### Send Payment UX

- **Unified Entry**: Single input for all payment types (paste/scan/upload)
- **Auto-Detect**: Parse input to determine type automatically
- **Fee Display**: Show fees and total before confirmation
- **All Funds**: Offer "Use all funds" option
- **Confirmation**: Clear confirmation screen before sending

### Payment Display UX

- **Separate Fees**: Show fees visually separate from amounts
- **Titles**: Use Lightning addresses over invoice descriptions
- **Status**: Clear pending/succeeded/failed indicators
- **Details**: Progressive disclosure for technical details (tx ID, etc.)

### Seed Management UX

- **Defer Backup**: Wait until after first received payment
- **Verification**: Require partial seed re-entry to verify backup
- **Recovery**: Support encrypted cloud backup options

---

## Production Checklist

### Required Implementations

- [ ] **Logging**: Implement `Logger` interface at DEBUG level
- [ ] **Events**: Implement `EventListener` for real-time updates
- [ ] **Cleanup**: Remove event listeners when disposing resources
- [ ] **Limits**: Call `FetchLightningLimits()` / `FetchOnchainLimits()` before payments

### Payment Status Handling

- [ ] Monitor `Payment.status` for state changes
- [ ] Handle `Pending`, `Complete`, `Failed` states appropriately
- [ ] Display clear status indicators to users

### Refund Management

- [ ] Check `ListRefundables()` regularly
- [ ] Allow users to retry refunds with higher fees
- [ ] Implement `RescanOnchainSwaps()` for recovery

### Fee Transparency

- [ ] Display fees in prepare responses before confirmation
- [ ] Show total amount (amount + fees) clearly
- [ ] Use fiat equivalents where helpful

### Error Handling

- [ ] Catch and handle SDK exceptions appropriately
- [ ] Provide user-friendly error messages
- [ ] Log detailed errors for debugging
- [ ] Handle network connectivity issues gracefully

---

## Common Patterns Summary

| Operation | Prepare Method | Execute Method | Check Limits |
|-----------|---------------|----------------|--------------|
| Receive Lightning | `PrepareReceivePayment()` | `ReceivePayment()` | `FetchLightningLimits()` |
| Receive On-chain | `PrepareReceivePayment()` | `ReceivePayment()` | `FetchOnchainLimits()` |
| Send Lightning | `PrepareSendPayment()` | `SendPayment()` | `FetchLightningLimits()` |
| Send On-chain | `PreparePayOnchain()` | `PayOnchain()` | `FetchOnchainLimits()` |
| LNURL-Pay | `PrepareLnurlPay()` | `LnurlPay()` | `FetchLightningLimits()` |

**Remember**: Always prepare, show fees, get user confirmation, then execute.
