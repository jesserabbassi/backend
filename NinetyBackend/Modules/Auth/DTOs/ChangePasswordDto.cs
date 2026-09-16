namespace NinetyBackend.Modules.Auth.DTOs;

public record ChangePasswordDto(string CurrentPassword, string NewPassword);
