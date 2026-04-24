using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _4Bet.Infrastructure.Domain;

public class Transaction : BaseEntity
{
    [Required]
    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } // Сума транзакції (позитивна або негативна)

    public string Description { get; set; } = string.Empty;

    // Зв'язок зі ставкою. Якщо транзакція стосується парі (тип BetPlaced/Won/Refunded),
    // тут зберігається ID відповідного запису з таблиці Bets.
    public Guid? ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}