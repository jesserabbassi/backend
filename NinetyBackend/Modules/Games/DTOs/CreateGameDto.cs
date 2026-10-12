namespace NinetyBackend.Modules.Games.DTOs;

public record CreateGameDto(
    string Name,
    string Publisher,
    string Version,
    string Description
);