namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;

/// <summary>
/// Handles late payments received on expired invoices.
/// </summary>
public interface ILatePaymentHandler
{
    /// <summary>
    /// Processes a late payment that was received after the invoice expired.
    /// Logs a warning and notifies administrators.
    /// </summary>
    Task HandleLatePaymentAsync(string paymentHash, CancellationToken cancellationToken = default);
}
