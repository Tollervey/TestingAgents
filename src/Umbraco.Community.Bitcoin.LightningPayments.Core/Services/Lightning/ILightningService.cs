namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Lightning
{
    public interface ILightningService
    {
        Task<string> GetPaymentStatusAsync();
    }
}


