using System.Diagnostics;
using System.Reflection;

namespace Breez.Sdk.Liquid.Extensions.Core.Observability;

/// <summary>
/// Provides OpenTelemetry activity sources and operation name constants for distributed tracing.
/// </summary>
/// <remarks>
/// This class centralizes the creation of activities for Breez SDK operations, enabling
/// observability through OpenTelemetry tracing. All SDK operations should use these
/// activity sources to ensure consistent telemetry collection.
/// </remarks>
public static class ActivitySources
{
    private static readonly string _version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "1.0.0";

    /// <summary>
    /// Gets the primary ActivitySource for Breez SDK Liquid Extensions.
    /// </summary>
    /// <remarks>
    /// This ActivitySource is used to create all activities for SDK operations.
    /// Configure OpenTelemetry to listen to "Breez.Sdk.Liquid.Extensions" to collect traces.
    /// </remarks>
    public static ActivitySource Source { get; } = new("Breez.Sdk.Liquid.Extensions", _version);

    /// <summary>
    /// Standard operation name constants for Breez SDK operations.
    /// </summary>
    /// <remarks>
    /// These constants follow OpenTelemetry semantic conventions for operation naming,
    /// using lowercase snake_case format with a "breez.sdk." prefix.
    /// </remarks>
    public static class Operations
    {
        /// <summary>
        /// Operation name for connecting to the Breez SDK.
        /// </summary>
        public static readonly string Connect = "breez.sdk.connect";

        /// <summary>
        /// Operation name for disconnecting from the Breez SDK.
        /// </summary>
        public static readonly string Disconnect = "breez.sdk.disconnect";

        /// <summary>
        /// Operation name for creating a Lightning invoice.
        /// </summary>
        public static readonly string CreateInvoice = "breez.sdk.create_invoice";

        /// <summary>
        /// Operation name for retrieving payment details.
        /// </summary>
        public static readonly string GetPayment = "breez.sdk.get_payment";

        /// <summary>
        /// Operation name for getting the current balance.
        /// </summary>
        public static readonly string GetBalance = "breez.sdk.get_balance";

        /// <summary>
        /// Operation name for sending a Lightning payment.
        /// </summary>
        public static readonly string SendPayment = "breez.sdk.send_payment";

        /// <summary>
        /// Operation name for listing payment history.
        /// </summary>
        public static readonly string ListPayments = "breez.sdk.list_payments";

        /// <summary>
        /// Operation name for preparing to receive a payment.
        /// </summary>
        public static readonly string PrepareReceive = "breez.sdk.prepare_receive";

        /// <summary>
        /// Operation name for preparing to send a payment.
        /// </summary>
        public static readonly string PrepareSend = "breez.sdk.prepare_send";
    }

    /// <summary>
    /// Starts a new activity for the specified operation.
    /// </summary>
    /// <param name="operationName">The name of the operation being traced.</param>
    /// <param name="kind">The kind of activity (default: Internal).</param>
    /// <returns>
    /// An Activity instance if listeners are active; otherwise, null.
    /// Callers should use 'using' to ensure proper disposal.
    /// </returns>
    /// <remarks>
    /// Activities are only created when an ActivityListener is registered and configured
    /// to listen to the "Breez.Sdk.Liquid.Extensions" source. In production, OpenTelemetry
    /// exporters typically register these listeners.
    /// </remarks>
    public static Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal)
    {
        return Source.StartActivity(operationName, kind);
    }

    /// <summary>
    /// Starts a new activity for the Connect operation.
    /// </summary>
    /// <returns>An Activity instance if listeners are active; otherwise, null.</returns>
    public static Activity? StartConnectActivity()
    {
        return StartActivity(Operations.Connect);
    }

    /// <summary>
    /// Starts a new activity for the Disconnect operation.
    /// </summary>
    /// <returns>An Activity instance if listeners are active; otherwise, null.</returns>
    public static Activity? StartDisconnectActivity()
    {
        return StartActivity(Operations.Disconnect);
    }

    /// <summary>
    /// Starts a new activity for the CreateInvoice operation.
    /// </summary>
    /// <returns>An Activity instance if listeners are active; otherwise, null.</returns>
    public static Activity? StartCreateInvoiceActivity()
    {
        return StartActivity(Operations.CreateInvoice);
    }

    /// <summary>
    /// Starts a new activity for the SendPayment operation.
    /// </summary>
    /// <returns>An Activity instance if listeners are active; otherwise, null.</returns>
    public static Activity? StartSendPaymentActivity()
    {
        return StartActivity(Operations.SendPayment);
    }

    /// <summary>
    /// Starts a new activity for the GetBalance operation.
    /// </summary>
    /// <returns>An Activity instance if listeners are active; otherwise, null.</returns>
    public static Activity? StartGetBalanceActivity()
    {
        return StartActivity(Operations.GetBalance);
    }
}
