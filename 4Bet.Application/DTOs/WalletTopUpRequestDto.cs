using System.ComponentModel.DataAnnotations;

namespace _4Bet.Application.DTOs;

public class WalletTopUpRequestDto
{
    [Range(0.01, 1_000_000)]
    public decimal Amount { get; set; }
}
