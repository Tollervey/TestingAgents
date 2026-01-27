namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Runtime
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}


