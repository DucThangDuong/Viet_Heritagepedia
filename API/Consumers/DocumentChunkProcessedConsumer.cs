using Application.Contracts;
using API.Hubs;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace API.Consumers;

public class DocumentChunkProcessedConsumer : IConsumer<DocumentChunkProcessedEvent>
{
    private readonly IHubContext<DocumentProcessingHub> _hubContext;

    public DocumentChunkProcessedConsumer(IHubContext<DocumentProcessingHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task Consume(ConsumeContext<DocumentChunkProcessedEvent> context)
    {
        var message = context.Message;
        string jobIdGroup = message.JobId.ToString();

        // Stream chunk / progress directly to WebSocket client connected to this JobId group
        await _hubContext.Clients.Group(jobIdGroup).SendAsync("ReceiveDocumentChunk", new
        {
            jobId = message.JobId,
            chunkIndex = message.ChunkIndex,
            totalChunks = message.TotalChunks,
            dataJson = message.DataJson,
            isCompleted = message.IsCompleted,
            errorMessage = message.ErrorMessage
        });
    }
}
