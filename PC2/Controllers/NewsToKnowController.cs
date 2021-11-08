using Microsoft.AspNetCore.Mvc;

namespace PC2.Controllers
{
    public class NewsToKnowController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
