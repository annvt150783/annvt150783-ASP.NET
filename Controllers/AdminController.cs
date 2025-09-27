using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimAuctionMVC.Data;
using SimAuctionMVC.Models;

namespace SimAuctionMVC.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private async Task<bool> IsAdminAsync()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        return currentUser?.Email == "admin@sim.vn";
    }

    public async Task<IActionResult> Index()
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        // Thống kê tổng quan
        var totalAuctions = await _context.Auctions.CountAsync();
        var activeAuctions = await _context.Auctions.CountAsync(a => a.Status == "active");
        var totalUsers = await _context.Users.CountAsync();
        var totalBids = await _context.Bids.CountAsync();

        ViewBag.TotalAuctions = totalAuctions;
        ViewBag.ActiveAuctions = activeAuctions;
        ViewBag.TotalUsers = totalUsers;
        ViewBag.TotalBids = totalBids;

        // Danh sách đấu giá gần đây
        var recentAuctions = await _context.Auctions
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync();

        return View(recentAuctions);
    }

    public async Task<IActionResult> Auctions()
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var auctions = await _context.Auctions
            .Include(a => a.Bids)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return View(auctions);
    }

    public async Task<IActionResult> Users()
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var users = await _context.Users
            .Select(u => new {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.CreatedAt,
                BidCount = _context.Bids.Count(b => b.UserId == u.Id)
            })
            .ToListAsync();

        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> EndAuction(int id)
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var auction = await _context.Auctions.FindAsync(id);
        if (auction == null)
        {
            return NotFound();
        }

        auction.Status = "completed";
        auction.EndTime = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAuction(int id)
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var auction = await _context.Auctions
            .Include(a => a.Bids)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (auction == null)
        {
            return NotFound();
        }

        _context.Bids.RemoveRange(auction.Bids);
        _context.Auctions.Remove(auction);
        await _context.SaveChangesAsync();

        return Ok();
    }

    public async Task<IActionResult> GetUserDetails(string id)
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var user = await _context.Users
            .Where(u => u.Id == id)
            .Select(u => new {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.CreatedAt,
                Bids = _context.Bids.Where(b => b.UserId == u.Id)
                    .Include(b => b.Auction)
                    .OrderByDescending(b => b.Timestamp)
                    .Take(10)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound();
        }

        return PartialView("_UserDetails", user);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null || user.Email == "admin@sim.vn")
        {
            return NotFound();
        }

        // Tạm thời chỉ trả về OK, có thể implement lockout sau
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteUser(string id)
    {
        if (!await IsAdminAsync())
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null || user.Email == "admin@sim.vn")
        {
            return NotFound();
        }
        
        // Xóa các bids của user trước
        var userBids = await _context.Bids.Where(b => b.UserId == id).ToListAsync();
        _context.Bids.RemoveRange(userBids);
        
        // Xóa user
        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            await _context.SaveChangesAsync();
            return Ok();
        }

        return BadRequest();
    }
}