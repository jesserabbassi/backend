using System.Text.Json.Serialization;

namespace NinetyGamingStationBackend.Realtime;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StartSessionCommand), "START_SESSION")]
[JsonDerivedType(typeof(EndSessionCommand), "END_SESSION")]
[JsonDerivedType(typeof(LockCommand), "LOCK")]
[JsonDerivedType(typeof(UnlockCommand), "UNLOCK")]
[JsonDerivedType(typeof(RestartCommand), "RESTART")]
[JsonDerivedType(typeof(ShutdownCommand), "SHUTDOWN")]
[JsonDerivedType(typeof(LaunchGameCommand), "LAUNCH_GAME")]
public abstract record BackendCommand(Guid CommandId);

public sealed record StartSessionCommand(Guid CommandId, string SessionId) : BackendCommand(CommandId);
public sealed record EndSessionCommand(Guid CommandId, string SessionId, string Reason) : BackendCommand(CommandId);
public sealed record LockCommand(Guid CommandId) : BackendCommand(CommandId);
public sealed record UnlockCommand(Guid CommandId) : BackendCommand(CommandId);
public sealed record RestartCommand(Guid CommandId) : BackendCommand(CommandId);
public sealed record ShutdownCommand(Guid CommandId) : BackendCommand(CommandId);
public sealed record LaunchGameCommand(Guid CommandId, string Executable) : BackendCommand(CommandId);
