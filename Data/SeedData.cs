using Microsoft.AspNetCore.Identity;
using SimAuctionMVC.Models;

namespace SimAuctionMVC.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider, UserManager<ApplicationUser> userManager)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed users if not exist
        if (!context.Users.Any())
        {
            var adminUser = new ApplicationUser
            {
                UserName = "admin@sim.vn",
                Email = "admin@sim.vn",
                FullName = "Quản trị viên",
                PhoneNumber = "0901234567",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, "Admin123!");

            var normalUser = new ApplicationUser
            {
                UserName = "user@sim.vn",
                Email = "user@sim.vn",
                FullName = "Nguyễn Văn Nam",
                PhoneNumber = "0987654321",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(normalUser, "User123!");
        }

        // Seed auctions if not exist
        if (!context.Auctions.Any())
        {
            var auctions = new List<Auction>
            {
                new Auction
                {
                    SimNumber = "0988888888",
                    Carrier = "Viettel",
                    Category = "Tứ Quý",
                    StartingPrice = 50000000,
                    CurrentPrice = 65000000,
                    StepPrice = 1000000,
                    StartTime = DateTime.Now.AddDays(-1),
                    EndTime = DateTime.Now.AddHours(1),
                    Status = "active",
                    Description = "SIM tứ quý 8 - Số may mắn đem lại thịnh vượng và tài lộc",
                    Image = "https://images.pexels.com/photos/404280/pexels-photo-404280.jpeg",
                    TotalBids = 12
                },
                new Auction
                {
                    SimNumber = "0977777777",
                    Carrier = "Mobifone",
                    Category = "Tứ Quý",
                    StartingPrice = 40000000,
                    CurrentPrice = 45000000,
                    StepPrice = 500000,
                    StartTime = DateTime.Now.AddHours(-12),
                    EndTime = DateTime.Now.AddHours(2),
                    Status = "active",
                    Description = "SIM tứ quý 7 - Con số thiêng liêng mang đến may mắn",
                    Image = "https://images.pexels.com/photos/1629236/pexels-photo-1629236.jpeg",
                    TotalBids = 8
                },
                new Auction
                {
                    SimNumber = "0913681368",
                    Carrier = "Vinaphone",
                    Category = "Lộc Phát",
                    StartingPrice = 25000000,
                    CurrentPrice = 30000000,
                    StepPrice = 500000,
                    StartTime = DateTime.Now.AddHours(-6),
                    EndTime = DateTime.Now.AddHours(3),
                    Status = "active",
                    Description = "SIM Lộc Phát 1368 - Ý nghĩa nhất sinh lộc phát",
                    Image = "https://images.pexels.com/photos/1138900/pexels-photo-1138900.jpeg",
                    TotalBids = 15
                }
            };

            context.Auctions.AddRange(auctions);
            await context.SaveChangesAsync();
        }
    }
}