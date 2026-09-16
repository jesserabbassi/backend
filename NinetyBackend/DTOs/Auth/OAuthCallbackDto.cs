namespace NinetyBackend.DTOs.Auth
{
    public record OAuthCallbackDto(string Code, string State);
}