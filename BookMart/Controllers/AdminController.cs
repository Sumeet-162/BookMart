using Microsoft.AspNetCore.Mvc;

namespace BookMart.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult dashboard()
        {
            return View();
        }
        public IActionResult allorder()
        {
            return View();
        }
        public IActionResult adminoperation()
        {
            return View();
        }
        public IActionResult adminstock()
        {
            return View();
        }
        public IActionResult adminupdatestock()
        {
            return View();
        }
    }
}
