namespace NinetyBackend.Modules.RemoteControl.Models;

public enum CommandType
{
    Lock,
    Unlock,
    Shutdown,
    Restart,
    StartSession,
    EndSession,
    LaunchGame
}

public enum CommandStatus
{
    Pending,
    Sent,
    Acknowledged,
    Failed,
    Expired
}

public class RemoteCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StationId { get; set; }
    public CommandType Type { get; set; }
    public CommandStatus Status { get; set; } = CommandStatus.Pending;
    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ResultMessage { get; set; }
}

public record CommandResultDto(
    Guid CommandId,
    Guid StationId,
    CommandType Type,
    CommandStatus Status,
    DateTime CreatedAt,
    DateTime? SentAt,
    DateTime? AcknowledgedAt,
    string? ResultMessage
);

public record CommandAcknowledgementDto(
    bool Success,
    string? Message
);
