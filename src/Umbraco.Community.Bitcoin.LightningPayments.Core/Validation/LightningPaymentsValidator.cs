using FluentValidation;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Validation;

/// <summary>
/// Base validator class for Lightning Payments API request validation.
/// Provides common validation rules and patterns.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
public abstract class LightningPaymentsValidator<T> : AbstractValidator<T>
{
    /// <summary>
    /// Maximum length for payment hash strings (64 hex characters).
    /// </summary>
    protected const int PaymentHashLength = 64;

    /// <summary>
    /// Maximum length for invoice strings.
    /// </summary>
    protected const int MaxInvoiceLength = 2000;

    /// <summary>
    /// Maximum length for description fields.
    /// </summary>
    protected const int MaxDescriptionLength = 200;

    /// <summary>
    /// Minimum amount in satoshis for payments.
    /// </summary>
    protected const ulong MinAmountSat = 1;

    /// <summary>
    /// Maximum amount in satoshis for a single payment (1 BTC).
    /// </summary>
    protected const ulong MaxAmountSat = 100_000_000;

    /// <summary>
    /// Validates that a string is a valid BOLT11 invoice format.
    /// </summary>
    protected static bool BeValidBolt11Invoice(string? invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice))
        {
            return false;
        }

        // BOLT11 invoices start with "ln" followed by network prefix
        // lnbc (mainnet), lntb (testnet), lnbcrt (regtest), lnsb (signet)
        var lowerInvoice = invoice.ToLowerInvariant();
        return lowerInvoice.StartsWith("lnbc") ||
               lowerInvoice.StartsWith("lntb") ||
               lowerInvoice.StartsWith("lnbcrt") ||
               lowerInvoice.StartsWith("lnsb");
    }

    /// <summary>
    /// Validates that a string is a valid BOLT12 offer format.
    /// </summary>
    protected static bool BeValidBolt12Offer(string? offer)
    {
        if (string.IsNullOrWhiteSpace(offer))
        {
            return false;
        }

        // BOLT12 offers start with "lno1"
        return offer.ToLowerInvariant().StartsWith("lno1");
    }

    /// <summary>
    /// Validates that a string is a valid payment hash (64 hex characters).
    /// </summary>
    protected static bool BeValidPaymentHash(string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        if (hash.Length != PaymentHashLength)
        {
            return false;
        }

        return hash.All(c => Uri.IsHexDigit(c));
    }

    /// <summary>
    /// Validates that a string is a valid ISO 4217 currency code.
    /// </summary>
    protected static bool BeValidCurrencyCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        if (code.Length != 3)
        {
            return false;
        }

        return code.All(char.IsLetter) && code.All(char.IsUpper);
    }
}

/// <summary>
/// Extension methods for FluentValidation rule builders.
/// </summary>
public static class LightningValidationExtensions
{
    /// <summary>
    /// Validates that a string is a valid BOLT11 invoice.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidBolt11Invoice<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Invoice is required.")
            .Must(invoice => ValidationHelper.BeValidBolt11Invoice(invoice))
            .WithMessage("Invoice must be a valid BOLT11 invoice starting with 'ln'.");
    }

    /// <summary>
    /// Validates that a string is a valid BOLT12 offer.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidBolt12Offer<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Offer string is required.")
            .Must(offer => ValidationHelper.BeValidBolt12Offer(offer))
            .WithMessage("Offer must be a valid BOLT12 offer starting with 'lno1'.");
    }

    /// <summary>
    /// Validates that a string is a valid payment hash.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidPaymentHash<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Payment hash is required.")
            .Must(hash => ValidationHelper.BeValidPaymentHash(hash))
            .WithMessage("Payment hash must be a 64-character hexadecimal string.");
    }

    /// <summary>
    /// Validates that a numeric value is within satoshi amount limits.
    /// </summary>
    public static IRuleBuilderOptions<T, ulong> MustBeValidSatoshiAmount<T>(
        this IRuleBuilder<T, ulong> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0UL).WithMessage("Amount must be greater than 0 satoshis.")
            .LessThanOrEqualTo(100_000_000UL).WithMessage("Amount cannot exceed 100,000,000 satoshis (1 BTC).");
    }

    /// <summary>
    /// Validates that a string is a valid ISO 4217 currency code.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidCurrencyCode<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Currency code is required.")
            .Must(code => ValidationHelper.BeValidCurrencyCode(code))
            .WithMessage("Currency code must be a valid 3-letter ISO 4217 code (e.g., USD, EUR, GBP).");
    }
}

/// <summary>
/// Internal helper class for validation logic.
/// </summary>
internal static class ValidationHelper
{
    private const int PaymentHashLength = 64;

    /// <summary>
    /// Validates that a string is a valid BOLT11 invoice format.
    /// </summary>
    internal static bool BeValidBolt11Invoice(string? invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice))
        {
            return false;
        }

        var lowerInvoice = invoice.ToLowerInvariant();
        return lowerInvoice.StartsWith("lnbc") ||
               lowerInvoice.StartsWith("lntb") ||
               lowerInvoice.StartsWith("lnbcrt") ||
               lowerInvoice.StartsWith("lnsb");
    }

    /// <summary>
    /// Validates that a string is a valid BOLT12 offer format.
    /// </summary>
    internal static bool BeValidBolt12Offer(string? offer)
    {
        if (string.IsNullOrWhiteSpace(offer))
        {
            return false;
        }

        return offer.ToLowerInvariant().StartsWith("lno1");
    }

    /// <summary>
    /// Validates that a string is a valid payment hash (64 hex characters).
    /// </summary>
    internal static bool BeValidPaymentHash(string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        if (hash.Length != PaymentHashLength)
        {
            return false;
        }

        return hash.All(c => Uri.IsHexDigit(c));
    }

    /// <summary>
    /// Validates that a string is a valid ISO 4217 currency code.
    /// </summary>
    internal static bool BeValidCurrencyCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        if (code.Length != 3)
        {
            return false;
        }

        return code.All(char.IsLetter) && code.All(char.IsUpper);
    }
}
