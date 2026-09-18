namespace NinetyBackend.Modules.Auth.DTOs;

    public record RegisterDto
    (
        string FirstName,
        string LastName,
        string Email,
        string Password
    );
