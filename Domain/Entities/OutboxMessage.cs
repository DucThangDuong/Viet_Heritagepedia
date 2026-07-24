using System;

namespace Domain.Entities;

public partial class OutboxMessage
{
    public Guid Id { get; set; }

    public string MessageType { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DateTime? ProcessedAt { get; set; }

    public DateTime? CreatedAt { get; set; }
}
