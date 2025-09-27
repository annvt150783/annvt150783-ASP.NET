using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimAuctionMVC.Data;
using SimAuctionMVC.Models;
using System.ComponentModel.DataAnnotations;

namespace SimAuctionMVC.Controllers;

[Authorize]
public class ManageController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public ManageController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var model = new ManageViewModel
        {
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber
        };

        // Lấy lịch sử đấu giá của user
        var userBids = await _context.Bids
            .Include(b => b.Auction)
            .Where(b => b.UserId == user.Id)
            .OrderByDescending(b => b.Timestamp)
            .Take(10)
            .ToListAsync();

        ViewBag.UserBids = userBids;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(ManageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
        }
        else
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return RedirectToAction("Index");
    }
}

public class ManageViewModel
{
    [Required(ErrorMessage = "Họ và tên là bắt buộc")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? PhoneNumber { get; set; }
}