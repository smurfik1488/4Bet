using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using _4Bet.Infrastructure.Domain;

namespace _4Bet.Application.Services;

public class BusinessRulesService : IBusinessRulesService
{
    public void EnsureVerified(User user, string actionName)
    {
        if (!user.IsBdVerified)
        {
            throw new InvalidOperationException($"Please verify your documents before {actionName}.");
        }
    }

    public void EnsurePositiveAmount(decimal amount, string actionName)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException($"{actionName} amount must be greater than zero.");
        }
    }

    public void EnsureValidOdds(double homeWinOdds, double drawOdds, double awayWinOdds)
    {
        if (homeWinOdds < 1.0 || drawOdds < 1.0 || awayWinOdds < 1.0)
        {
            throw new InvalidOperationException("Odds must be >= 1.00.");
        }
    }

    public void ValidateSportEventInput(ManageSportEventDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.HomeTeam) || string.IsNullOrWhiteSpace(dto.AwayTeam))
        {
            throw new InvalidOperationException("Home team and away team are required.");
        }

        if (dto.HomeTeam.Trim().Equals(dto.AwayTeam.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Home team and away team must be different.");
        }

        if (dto.EventDate.Year < 2000 || dto.EventDate.Year > 2100)
        {
            throw new InvalidOperationException("Event date is out of acceptable range.");
        }

        if (dto.MatchMinute is < 0 or > 130)
        {
            throw new InvalidOperationException("Match minute must be between 0 and 130.");
        }

        if (dto.HomeScore is < 0 || dto.AwayScore is < 0)
        {
            throw new InvalidOperationException("Score cannot be negative.");
        }
    }

    public string NormalizeMatchStatus(string? status, string fallback = "NS")
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return fallback;
        }

        return status.Trim().ToUpperInvariant();
    }
}
