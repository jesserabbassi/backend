# Rust Agent SignalR contract

Hub URL: `/hubs/agents`.

Use the existing JWT bearer authentication. Send the JWT in the existing `accessToken` HttpOnly cookie. The JWT must contain `agent_id`, matching the persisted `Agent.Id`. Anonymous connections, missing identities, and wrong station assignments are rejected.

1. Connect to `/hubs/agents`.
2. Invoke `ConnectAgent(stationId)`.
3. Invoke `Heartbeat()` every 15 seconds.
4. Invoke `SendTelemetry(telemetry)` no faster than once per second.
5. Reconnect with the same JWT and station ID after disconnect.

Telemetry payload:

```json
{"stationId":"guid","cpuUsage":0,"gpuUsage":0,"ramUsage":0,"cpuTemperature":0,"gpuTemperature":0,"fanSpeed":0,"networkStatus":"Connected","timestamp":"2026-01-01T00:00:00Z"}
```

Server events: `ConnectionAccepted`, `StationStatusChanged`, `ServerNotification` (`HeartbeatAccepted`). Errors: unauthenticated, missing agent identity, unassigned station, invalid telemetry, or rate limit exceeded.

The hub delegates presence to `IAgentService` and telemetry to `IMonitoringService`; it does not access the database directly.
