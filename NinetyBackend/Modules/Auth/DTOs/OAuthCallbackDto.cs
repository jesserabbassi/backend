namespace NinetyBackend.Modules.Auth.DTOs;

public record OAuthCallbackDto(string Code, string State);
