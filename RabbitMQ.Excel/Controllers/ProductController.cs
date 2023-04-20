using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Excel.Enums;
using RabbitMQ.Excel.Models;
using RabbitMQ.Excel.Services;
using Shared;

namespace RabbitMQ.Excel.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppDbContext _appDbContext;
        private readonly RabbitMQPublisher _rabbitMQPublisher;

        public ProductController(UserManager<IdentityUser> userManager, AppDbContext appDbContext, RabbitMQPublisher rabbitMQPublisher)
        {
            _userManager = userManager;
            _appDbContext = appDbContext;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> CreateProductExcel()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var fileName = $"product-excel-{Guid.NewGuid().ToString().Substring(1, 10)}";
            UserFile userFile = new()
            {
                UserId = user.Id,
                FileName = fileName,
                FileStatus = FileStatus.Creating
            };

            await _appDbContext.UserFiles.AddAsync(userFile);
            await _appDbContext.SaveChangesAsync();

            _rabbitMQPublisher.Publish(new CreateExcelMessage() { FileId = userFile.Id });

            TempData["StartCreatingExcel"] = true;
            return RedirectToAction(nameof(Files));
        }

        public async Task<IActionResult> Files()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            return View(await _appDbContext.UserFiles.Where(x => x.UserId == user.Id).OrderByDescending(x => x.Id).ToListAsync());
        }
    }
}