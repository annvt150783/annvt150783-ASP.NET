using System.ComponentModel.DataAnnotations;

namespace SimAuctionMVC.Models;

public class Auction
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(15)]
    public string SimNumber { get; set; } = string.Empty;
    
    [Required]
    public string Carrier { get; set; } = string.Empty; // Viettel, Mobifone, Vinaphone
    
    [Required]
    public string Category { get; set; } = string.Empty; // Tứ Quý, Lộc Phát, Thần Tài, Năm Sinh, Khác
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal StartingPrice { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal CurrentPrice { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal StepPrice { get; set; }
    
    [Required]
    public DateTime StartTime { get; set; } = DateTime.Now;
    
    [Required]
    public DateTime EndTime { get; set; }
    
    [Required]
    public string Status { get; set; } = "upcoming"; // upcoming, active, ended
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public string? Image { get; set; }
    
    public int TotalBids { get; set; } = 0;
    
    public string? WinnerId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ApplicationUser? Winner { get; set; }
    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();
}