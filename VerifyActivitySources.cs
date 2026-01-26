using System;
using System.Collections.Generic;
using System.Diagnostics;
using Breez.Sdk.Liquid.Extensions.Core.Observability;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing ActivitySources implementation...\n");

        // Test 1: ActivitySource exists and has correct name
        Console.WriteLine("Test 1: ActivitySource name");
        var source = ActivitySources.Source;
        Console.WriteLine($"  Source name: {source.Name}");
        Console.WriteLine($"  Expected: Breez.Sdk.Liquid.Extensions");
        Console.WriteLine($"  Pass: {source.Name == "Breez.Sdk.Liquid.Extensions"}\n");

        // Test 2: ActivitySource has version
        Console.WriteLine("Test 2: ActivitySource version");
        Console.WriteLine($"  Version: {source.Version}");
        Console.WriteLine($"  Pass: {!string.IsNullOrEmpty(source.Version)}\n");

        // Test 3: Operation constants
        Console.WriteLine("Test 3: Operation name constants");
        Console.WriteLine($"  Connect: {ActivitySources.Operations.Connect} (expected: breez.sdk.connect)");
        Console.WriteLine($"  Pass: {ActivitySources.Operations.Connect == "breez.sdk.connect"}");
        Console.WriteLine($"  Disconnect: {ActivitySources.Operations.Disconnect} (expected: breez.sdk.disconnect)");
        Console.WriteLine($"  Pass: {ActivitySources.Operations.Disconnect == "breez.sdk.disconnect"}");
        Console.WriteLine($"  CreateInvoice: {ActivitySources.Operations.CreateInvoice} (expected: breez.sdk.create_invoice)");
        Console.WriteLine($"  Pass: {ActivitySources.Operations.CreateInvoice == "breez.sdk.create_invoice"}");
        Console.WriteLine($"  GetPayment: {ActivitySources.Operations.GetPayment} (expected: breez.sdk.get_payment)");
        Console.WriteLine($"  Pass: {ActivitySources.Operations.GetPayment == "breez.sdk.get_payment"}");
        Console.WriteLine($"  GetBalance: {ActivitySources.Operations.GetBalance} (expected: breez.sdk.get_balance)");
        Console.WriteLine($"  Pass: {ActivitySources.Operations.GetBalance == "breez.sdk.get_balance"}");
        Console.WriteLine($"  SendPayment: {ActivitySources.Operations.SendPayment} (expected: breez.sdk.send_payment)");
        Console.WriteLine($"  Pass: {ActivitySources.Operations.SendPayment == "breez.sdk.send_payment"}");
        Console.WriteLine($"  ListPayments: {ActivitySources.Operations.ListPayments} (expected: breez.sdk.list_payments)");
        Console.WriteLine($"  Pass: {ActivitySources.Operations.ListPayments == "breez.sdk.list_payments"}");
        Console.WriteLine($"  PrepareReceive: {ActivitySources.Operations.PrepareReceive} (expected: breez.sdk.prepare_receive)");
        Console.WriteLine($"  Pass: {ActivitySources.Operations.PrepareReceive == "breez.sdk.prepare_receive"}");
        Console.WriteLine($"  PrepareSend: {ActivitySources.Operations.PrepareSend} (expected: breez.sdk.prepare_send)");
        Console.WriteLine($"  Pass: {ActivitySources.Operations.PrepareSend == "breez.sdk.prepare_send"}\n");

        // Test 4: Activity creation with listener
        Console.WriteLine("Test 4: Activity creation with listener");
        var recordedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Breez.Sdk.Liquid.Extensions",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => recordedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        using (var activity = ActivitySources.StartActivity(ActivitySources.Operations.Connect))
        {
            Console.WriteLine($"  Activity created: {activity != null}");
            Console.WriteLine($"  Operation name: {activity?.OperationName}");
        }
        Console.WriteLine($"  Recorded activities count: {recordedActivities.Count}");
        Console.WriteLine($"  Pass: {recordedActivities.Count == 1}\n");

        // Test 5: Helper methods
        Console.WriteLine("Test 5: Helper methods");
        recordedActivities.Clear();

        using (var connectActivity = ActivitySources.StartConnectActivity())
        {
            Console.WriteLine($"  StartConnectActivity: {connectActivity?.OperationName == "breez.sdk.connect"}");
        }

        using (var disconnectActivity = ActivitySources.StartDisconnectActivity())
        {
            Console.WriteLine($"  StartDisconnectActivity: {disconnectActivity?.OperationName == "breez.sdk.disconnect"}");
        }

        using (var createInvoiceActivity = ActivitySources.StartCreateInvoiceActivity())
        {
            Console.WriteLine($"  StartCreateInvoiceActivity: {createInvoiceActivity?.OperationName == "breez.sdk.create_invoice"}");
        }

        using (var sendPaymentActivity = ActivitySources.StartSendPaymentActivity())
        {
            Console.WriteLine($"  StartSendPaymentActivity: {sendPaymentActivity?.OperationName == "breez.sdk.send_payment"}");
        }

        using (var getBalanceActivity = ActivitySources.StartGetBalanceActivity())
        {
            Console.WriteLine($"  StartGetBalanceActivity: {getBalanceActivity?.OperationName == "breez.sdk.get_balance"}");
        }

        Console.WriteLine($"\n  Total activities recorded: {recordedActivities.Count}");
        Console.WriteLine($"  Pass: {recordedActivities.Count == 5}\n");

        Console.WriteLine("All manual tests completed successfully!");
    }
}
