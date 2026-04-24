namespace _4Bet.Infrastructure.Domain;

public enum BetStatus
{
    Pending,    // Очікує результату матчу
    Won,        // Виграна
    Lost,       // Програна
    Refunded    // Повернення коштів
}
