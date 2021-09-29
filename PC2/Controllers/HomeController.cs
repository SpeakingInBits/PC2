using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Office.Interop.Excel;
using PC2.Data;
using PC2.Models;
using System.Diagnostics;

namespace PC2.Controllers
{
    public class HomeController : Controller
    {
        private const int CategoryStart = 28;
        private const int CategoryEnd = 99;
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

        public IActionResult About()
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

        //[Authorize(Roles = IdentityHelper.Admin)]
        public IActionResult ConfirmImportExcel()
        {
            return View();
        }

        [HttpPost]
        [ActionName("ConfirmImportExcel")]
        public async Task<IActionResult> ImportExcel()
        {
            Application xlApp = new Application();
            Workbook xlWorkbook = xlApp.Workbooks.Open(@"~/Data/Resources/2020.rg.breakout.xlsx");
            _Worksheet xlWorksheet = (_Worksheet)xlWorkbook.Sheets[0];
            Microsoft.Office.Interop.Excel.Range xlRange = xlWorksheet.UsedRange;
            await CreateAgencyCategories(xlRange);

            await CreateAgencies(xlRange);
            await AddCategoriesToAgency(xlRange);

            return View();
        }

        private async Task AddCategoriesToAgency(Microsoft.Office.Interop.Excel.Range xlRange)
        {
            List<AgencyCategory> categories = await AgencyCategoryDB.GetAgencyCategoriesAsync(_context);
            List<Agency> agencies = await AgencyDB.GetAllAgencyAsync(_context);

            for (int row = 2; row <= 901; row++)
            {
                for (int col = CategoryStart; col <= CategoryEnd; col++)
                {
                    if ((string)xlRange.Cells[row, col] == "x")
                    {
                        agencies[row - 1].AgencyCategories.Add(categories[col - CategoryStart]);
                    }
                }

            }
        }

        private async Task CreateAgencyCategories(Microsoft.Office.Interop.Excel.Range xlRange)
        {
            for (int col = CategoryStart; col <= CategoryEnd; col++)
            {
                AgencyCategory agencyCategory = new AgencyCategory();
                agencyCategory.AgencyCategoryName = (string)xlRange.Cells[1, col];
                await AgencyCategoryDB.AddCategoryAsync(_context, agencyCategory);
            }
        }

        private async Task CreateAgencies(Microsoft.Office.Interop.Excel.Range xlRange)
        {
            for (int row = 2; row <= 901; row++)
            {
                Agency agency = new Agency();
                for (int col = 1; col < CategoryStart; col++)
                {
                    switch (col)
                    {
                        case 1:
                            agency.AgencyName = (string)xlRange.Cells[row, col];
                            break;
                        case 2:
                            agency.AgencyName2 = (string)xlRange.Cells[row, col];
                            break;
                        case 3:
                            agency.Contact = (string)xlRange.Cells[row, col];
                            break;
                        case 4:
                            agency.Address1 = (string)xlRange.Cells[row, col];
                            break;
                        case 5:
                            agency.Address2 = (string)xlRange.Cells[row, col];
                            break;
                        case 6:
                            agency.City = (string)xlRange.Cells[row, col];
                            break;
                        case 7:
                            agency.State = (string)xlRange.Cells[row, col];
                            break;
                        case 8:
                            agency.Zip = (string)xlRange.Cells[row, col];
                            break;
                        case 9:
                            agency.MailingAddress = (string)xlRange.Cells[row, col];
                            break;
                        case 10:
                            agency.Phone = (string)xlRange.Cells[row, col];
                            break;
                        case 11:
                            if ((string)xlRange.Cells[row, col] != String.Empty)
                            {
                                agency.Phone += " " + (string)xlRange.Cells[row, col];
                            }
                            break;
                        case 12:
                            if ((string)xlRange.Cells[row, col] != String.Empty)
                            {
                                agency.Phone += " " + (string)xlRange.Cells[row, col];
                            }
                            break;
                        case 13:
                            if ((string)xlRange.Cells[row, col] != String.Empty)
                            {
                                agency.Phone += " " + (string)xlRange.Cells[row, col];
                            }
                            break;
                        case 14:
                            agency.TollFree = (string)xlRange.Cells[row, col];
                            break;
                        case 15:
                            if ((string)xlRange.Cells[row, col] != String.Empty)
                            {
                                agency.TollFree += " " + (string)xlRange.Cells[row, col];
                            }
                            break;
                        case 16:
                            agency.TTY = (string)xlRange.Cells[row, col];
                            break;
                        case 17:
                            if ((string)xlRange.Cells[row, col] != String.Empty)
                            {
                                agency.TTY += " " + (string)xlRange.Cells[row, col];
                            }
                            break;
                        case 18:
                            agency.TDD = (string)xlRange.Cells[row, col];
                            break;
                        case 19:
                            agency.CrisisHelpHotline = (string)xlRange.Cells[row, col];
                            break;
                        case 20:
                            agency.Fax = (string)xlRange.Cells[row, col];
                            break;
                        case 21:
                            if ((string)xlRange.Cells[row, col] != String.Empty)
                            {
                                agency.Fax += " " + (string)xlRange.Cells[row, col];
                            }
                            break;
                        case 22:
                            agency.Email = (string)xlRange.Cells[row, col];
                            break;
                        case 23:
                            if ((string)xlRange.Cells[row, col] != String.Empty)
                            {
                                agency.Email += " " + (string)xlRange.Cells[row, col];
                            }
                            break;
                        case 24:
                            if ((string)xlRange.Cells[row, col] != String.Empty)
                            {
                                agency.Email += " " + (string)xlRange.Cells[row, col];
                            }
                            break;
                        case 25:
                            agency.Website = (string)xlRange.Cells[row, col];
                            break;
                        case 26:
                            if ((string)xlRange.Cells[row, col] != String.Empty)
                            {
                                agency.Website += " " + (string)xlRange.Cells[row, col];
                            }
                            break;
                        case 27:
                            agency.Description = (string)xlRange.Cells[row, col];
                            break;
                    }
                }
                await AgencyDB.AddAgencyAsync(_context, agency);
            }
        }
    }
}