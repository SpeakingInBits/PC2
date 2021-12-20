using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CSharp.RuntimeBinder;
using OfficeOpenXml;
using PC2.Data;
using PC2.Models;
using System.Diagnostics;

namespace PC2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> About()
        {
            AboutUsModel aboutUs = new AboutUsModel();
            aboutUs.Staff = await StaffDB.GetAllStaff(_context);
            aboutUs.Board = await BoardDB.GetAllBoardMembers(_context);
            aboutUs.SteeringCommittee = await SteeringCommitteeDB.GetAllSteeringCommittee(_context);
            return View(aboutUs);
        }

        public IActionResult HousingProgram()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}