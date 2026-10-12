using NinetyBackend.Modules.Games.Models;

namespace NinetyBackend.Modules.Games.DTOs;

public record GameResponseDto(
    Guid Id,
    string Name,
    string Publisher,
    string Version,
    string Description,
    GameStatus Status
);