using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using System.Threading;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Realtime.Services
{
    public interface ISseHub
    {
        SseHub.SseClient AddClient(string sessionId);
        void RemoveClient(string sessionId, System.Guid clientId);
        void Broadcast(string sessionId, string @event, object payload);
        void BroadcastAll(string @event, object payload);
        void SendHeartbeat(string sessionId);
        void SendHeartbeatAll();
        static System.Threading.Tasks.Task WriteStreamAsync(HttpResponse response, ChannelReader<string> reader, CancellationToken ct) => SseHub.WriteStreamAsync(response, reader, ct);
    }
}