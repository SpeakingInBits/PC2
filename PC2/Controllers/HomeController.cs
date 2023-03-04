using IdentityLogin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;
using SendGrid;
using System.Diagnostics;

namespace PC2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender; 

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IEmailSender emailSender)
        {
            _logger = logger;
            _context = context;
            _emailSender = emailSender;
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
        
        public async Task<IActionResult> HousingProgram()
        {
            var housingData = await _context.HousingProgram
                                .OrderBy(data => data.HouseHoldSize)
                                .ToListAsync();
            return View(housingData);
        }

        public IActionResult PartnersForHousing()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult ContactPage()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ContactPage(ContactPageModel contactPageModel)
        {
            if (ModelState.IsValid)
            {
                Response result = await _emailSender.SendEmailAsync(contactPageModel.Name, contactPageModel.Email, contactPageModel.Phone, contactPageModel.Subject, contactPageModel.Message);
                ViewData["EmailSent"] = result.IsSuccessStatusCode;
                if (result.IsSuccessStatusCode)
                {
                    // Clear the data from the model
                    ModelState.Clear(); 
                    return View(new ContactPageModel());
                }
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}