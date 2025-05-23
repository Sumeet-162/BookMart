using Microsoft.AspNetCore.Mvc;

namespace BookMart.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult login()
        {
            return View();
        }
    }
}
