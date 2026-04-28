using _4Bet.Application.DTOs;
using _4Bet.Infrastructure.Domain;

namespace _4Bet.Application.IServices;

public interface IBusinessRulesService
{
    void EnsureVerified(User user, string actionName);
    void EnsurePositiveAmount(decimal amount, string actionName);
    void EnsureValidOdds(double homeWinOdds, double drawOdds, double awayWinOdds);
    void ValidateSportEventInput(ManageSportEventDto dto);
    string NormalizeMatchStatus(string? status, string fallback = "NS");
}
