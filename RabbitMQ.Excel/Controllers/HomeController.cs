using Microsoft.AspNetCore.Mvc;

namespace RabbitMQ.Excel.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}