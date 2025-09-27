using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimAuctionMVC.Data;
using SimAuctionMVC.Models;

namespace SimAuctionMVC.Controllers;

public class AuctionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuctionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private async Task<bool> IsAdminAsync()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        return currentUser?.Email == "admin@sim.vn";
    }

    public async Task<IActionResult> Index(string search, string carrier, string category, string priceRange, string sortBy = "endTime")
    {
        var auctions = _context.Auctions.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(search))
        {
            auctions = auctions.Where(a => a.SimNumber.Contains(search) || a.Description.Contains(search));
        }

        if (!string.IsNullOrEmpty(carrier))
        {
            auctions = auctions.Where(a => a.Carrier == carrier);
        }

        if (!string.IsNullOrEmpty(category))
        {
            auctions = auctions.Where(a => a.Category == category);
        }

        if (!string.IsNullOrEmpty(priceRange))
        {
            var parts = priceRange.Split('-');
            if (parts.Length == 2)
            {
                if (decimal.TryParse(parts[0], out var min))
                {
                    auctions = auctions.Where(a => a.CurrentPrice >= min * 1000000);
                }
                if (decimal.TryParse(parts[1], out var max))
                {
                    auctions = auctions.Where(a => a.CurrentPrice <= max * 1000000);
                }
            }
            else if (parts.Length == 1 && parts[0].EndsWith("-"))
            {
                if (decimal.TryParse(parts[0].TrimEnd('-'), out var min))
                {
                    auctions = auctions.Where(a => a.CurrentPrice >= min * 1000000);
                }
            }
        }

        // Apply sorting
        auctions = sortBy switch
        {
            "price-asc" => auctions.OrderBy(a => a.CurrentPrice),
            "price-desc" => auctions.OrderByDescending(a => a.CurrentPrice),
            "bids" => auctions.OrderByDescending(a => a.TotalBids),
            _ => auctions.OrderBy(a => a.EndTime)
        };

        var result = await auctions.ToListAsync();

        ViewBag.Search = search;
        ViewBag.Carrier = carrier;
        ViewBag.Category = category;
        ViewBag.PriceRange = priceRange;
        ViewBag.SortBy = sortBy;

        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var auction = await _context.Auctions
            .Include(a => a.Bids.OrderByDescending(b => b.Timestamp))
            .ThenInclude(b => b.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction == null)
        {
            return NotFound();
        }

        // Cập nhật CurrentPrice từ bid cao nhất
        if (auction.Bids.Any())
        {
            auction.CurrentPrice = auction.Bids.Max(b => b.Amount);
            auction.TotalBids = auction.Bids.Count;
        }

        return View(auction);
    }

    [Authorize]
    public async Task<IActionResult> Create()
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Auction auction)
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            auction.CreatedAt = DateTime.Now;
            auction.CurrentPrice = auction.StartingPrice;
            auction.Status = "active";
            auction.TotalBids = 0;

            _context.Auctions.Add(auction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tạo đấu giá thành công!";
            return RedirectToAction("Details", new { id = auction.Id });
        }

        return View(auction);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceBid(int auctionId, decimal amount)
    {
        var auction = await _context.Auctions
            .Include(a => a.Bids)
            .FirstOrDefaultAsync(a => a.Id == auctionId);

        if (auction == null)
        {
            return Json(new { success = false, message = "Không tìm thấy phiên đấu giá" });
        }

        if (auction.Status != "active")
        {
            return Json(new { success = false, message = "Phiên đấu giá đã kết thúc" });
        }

        if (auction.EndTime <= DateTime.Now)
        {
            return Json(new { success = false, message = "Phiên đấu giá đã hết hạn" });
        }

        var currentPrice = auction.Bids.Any() ? auction.Bids.Max(b => b.Amount) : auction.StartingPrice;
        var minimumBid = currentPrice + auction.StepPrice;

        if (amount < minimumBid)
        {
            return Json(new { success = false, message = $"Giá đấu tối thiểu là {minimumBid:N0} VNĐ" });
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập" });
        }

        var bid = new Bid
        {
            AuctionId = auctionId,
            UserId = userId,
            Amount = amount,
            Timestamp = DateTime.Now
        };

        _context.Bids.Add(bid);
        
        // Cập nhật thông tin auction
        auction.CurrentPrice = amount;
        auction.TotalBids = auction.Bids.Count + 1;
        
        await _context.SaveChangesAsync();

        return Json(new { 
            success = true, 
            message = "Đấu giá thành công!",
            newPrice = amount.ToString("N0"),
            totalBids = auction.TotalBids,
            nextMinimum = (amount + auction.StepPrice).ToString("N0")
        });
    }

    public async Task<IActionResult> GetBidHistory(int auctionId)
    {
        var bids = await _context.Bids
            .Include(b => b.User)
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.Timestamp)
            .ToListAsync();

        return PartialView("_BidHistory", bids);
    }
}