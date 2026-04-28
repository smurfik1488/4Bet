namespace _4Bet.Application.DTOs;

public class BetAnalyticsDto
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public int TotalBets { get; set; }
    public decimal TotalStake { get; set; }
    public decimal TotalPayout { get; set; }
    public decimal Net { get; set; }
    public double WinRatePercent { get; set; }
    public List<BetAnalyticsPointDto> Points { get; set; } = new();
}

public class BetAnalyticsPointDto
{
    public DateTime DayUtc { get; set; }
    public int BetsCount { get; set; }
    public int WonCount { get; set; }
    public int LostCount { get; set; }
    public decimal StakeSum { get; set; }
    public decimal PayoutSum { get; set; }
    public decimal Net { get; set; }
}
