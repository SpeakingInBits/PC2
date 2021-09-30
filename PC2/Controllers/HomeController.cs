using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CSharp.RuntimeBinder;
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
        private const int StartingRow = 2;
        private const int EndingRow = 901;
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
            string path = Path.Combine(Environment.CurrentDirectory, @"Data\Resources", "2020.rg.breakout.xlsx");
            Application xlApp = new Application();
            Workbook xlWorkbook = xlApp.Workbooks.Open(path);
            _Worksheet xlWorksheet = (_Worksheet)xlWorkbook.Sheets[1];
            Microsoft.Office.Interop.Excel.Range xlRange = xlWorksheet.UsedRange;
            await CreateAgencyCategories(xlRange);

            await CreateAgencies(xlRange);
            await AddCategoriesToAgency(xlRange);

            return View();
        }

        /// <summary>
        /// Creates the relationship between Agency and AgencyCategory
        /// </summary>
        private async Task AddCategoriesToAgency(Microsoft.Office.Interop.Excel.Range xlRange)
        {
            List<AgencyCategory> categories = await AgencyCategoryDB.GetAgencyCategoriesAsync(_context);
            List<Agency> agencies = await AgencyDB.GetAllAgencyAsync(_context);

            for (int row = StartingRow; row <= EndingRow; row++)
            {
                List<AgencyCategory> currentCategories = new List<AgencyCategory>();
                for (int col = CategoryStart; col <= CategoryEnd; col++)
                {
                    Microsoft.Office.Interop.Excel.Range range = xlRange.Cells[row, col];
                    try
                    {
                        if (range.Value.ToString() == "x")
                        {
                            agencies[row - 2].AgencyCategories.Add(categories[col - CategoryStart]);
                        }
                    }
                    catch (RuntimeBinderException)
                    {
                        continue;
                    }
                }
                await AgencyDB.UpdateAgencyAsync(_context, agencies[row - 2], currentCategories);
            }
        }

        /// <summary>
        /// Creates the Categories for the agencies
        /// </summary>
        private async Task CreateAgencyCategories(Microsoft.Office.Interop.Excel.Range xlRange)
        {
            for (int col = CategoryStart; col <= CategoryEnd; col++)
            {
                AgencyCategory agencyCategory = new AgencyCategory();
                Microsoft.Office.Interop.Excel.Range range = xlRange.Cells[1, col];
                agencyCategory.AgencyCategoryName = range.Value.ToString();
                await AgencyCategoryDB.AddCategoryAsync(_context, agencyCategory);
            }
        }

        /// <summary>
        /// Creates the agencies
        /// </summary>
        private async Task CreateAgencies(Microsoft.Office.Interop.Excel.Range xlRange)
        {
            for (int row = StartingRow; row <= EndingRow; row++)
            {
                Agency agency = new Agency();
                for (int col = 1; col < CategoryStart; col++)
                {
                    Microsoft.Office.Interop.Excel.Range range = xlRange.Cells[row, col];
                    try
                    {
                        switch (col)
                        {
                            case 1:
                                agency.AgencyName = range.Value.ToString();
                                break;
                            case 2:
                                if (range.Value.ToString() != null)
                                {
                                    agency.AgencyName2 = range.Value.ToString();
                                }
                                break;
                            case 3:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Contact = range.Value.ToString();
                                }
                                break;
                            case 4:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Address1 = range.Value.ToString();
                                }
                                break;
                            case 5:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Address2 = range.Value.ToString();
                                }
                                break;
                            case 6:
                                if (range.Value.ToString() != null)
                                {
                                    agency.City = range.Value.ToString();
                                }
                                break;
                            case 7:
                                if (range.Value.ToString() != null)
                                {
                                    agency.State = range.Value.ToString();
                                }
                                break;
                            case 8:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Zip = range.Value.ToString();
                                }
                                break;
                            case 9:
                                if (range.Value.ToString() != null)
                                {
                                    agency.MailingAddress = range.Value.ToString();
                                }
                                break;
                            case 10:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Phone = range.Value.ToString();
                                }
                                break;
                            case 11:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Phone += " " + range.Value.ToString();
                                }
                                break;
                            case 12:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Phone += " " + range.Value.ToString();
                                }
                                break;
                            case 13:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Phone += " " + range.Value.ToString();
                                }
                                break;
                            case 14:
                                if (range.Value.ToString() != null)
                                {
                                    agency.TollFree = range.Value.ToString();
                                }
                                break;
                            case 15:
                                if (range.Value.ToString() != null)
                                {
                                    agency.TollFree += " " + range.Value.ToString();
                                }
                                break;
                            case 16:
                                if (range.Value.ToString() != null)
                                {
                                    agency.TTY = range.Value.ToString();
                                }
                                break;
                            case 17:
                                if (range.Value.ToString() != null)
                                {
                                    agency.TTY += " " + range.Value.ToString();
                                }
                                break;
                            case 18:
                                if (range.Value.ToString() != null)
                                {
                                    agency.TDD = range.Value.ToString();
                                }
                                break;
                            case 19:
                                if (range.Value.ToString() != null)
                                {
                                    agency.CrisisHelpHotline = range.Value.ToString();
                                }
                                break;
                            case 20:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Fax = range.Value.ToString();
                                }
                                break;
                            case 21:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Fax += " " + range.Value.ToString();
                                }
                                break;
                            case 22:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Email = range.Value.ToString();
                                }
                                break;
                            case 23:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Email += " " + range.Value.ToString();
                                }
                                break;
                            case 24:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Email += " " + range.Value.ToString();
                                }
                                break;
                            case 25:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Website = range.Value.ToString();
                                }
                                break;
                            case 26:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Website += " " + range.Value.ToString();
                                }
                                break;
                            case 27:
                                if (range.Value.ToString() != null)
                                {
                                    agency.Description = range.Value.ToString();
                                }
                                break;
                        }
                    }
                    catch (RuntimeBinderException)
                    {
                        continue;
                    }
                }
                await AgencyDB.AddAgencyAsync(_context, agency);
            }
        }
    }
}