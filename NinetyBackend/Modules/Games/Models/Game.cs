namespace NinetyBackend.Modules.Games.Models;

public class Game
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GameStatus Status { get; set; } = GameStatus.ACTIVE;
}

public enum GameStatus
{
    ACTIVE,
    INACTIVE
}