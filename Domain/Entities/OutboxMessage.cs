using System;

namespace Domain.Entities;

public partial class OutboxMessage
{
    public Guid Id { get; private set; }

    public string MessageType { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public DateTime? ProcessedAt { get; private set; }

    public DateTime? CreatedAt { get; private set; }

    private OutboxMessage() { } // For EF Core

    public OutboxMessage(string messageType, string payload)
    {
        Id = Guid.NewGuid();
        MessageType = messageType;
        Payload = payload;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }
}
