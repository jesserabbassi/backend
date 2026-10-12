namespace NinetyBackend.Modules.Stations.DTOs;

public record AgentTokenResponseDto(
    Guid AgentId,
    Guid StationId,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);

public record AgentRefreshTokenRequestDto(string RefreshToken);
