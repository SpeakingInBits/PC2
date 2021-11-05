using Microsoft.AspNetCore.Mvc;

namespace PC2.Controllers
{
    public class ProgramVideosController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
