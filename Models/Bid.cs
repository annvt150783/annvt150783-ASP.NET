using System.ComponentModel.DataAnnotations;

namespace SimAuctionMVC.Models;

public class Bid
{
    public int Id { get; set; }
    
    [Required]
    public int AuctionId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Auction Auction { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
}