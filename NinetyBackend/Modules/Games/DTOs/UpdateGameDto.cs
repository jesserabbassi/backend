namespace NinetyBackend.Modules.Games.DTOs;

public record UpdateGameDto(
    string Name,
    string Publisher,
    string Version,
    string Description
);