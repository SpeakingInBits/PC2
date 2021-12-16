// See https://aka.ms/new-console-template for more information
using ImportExcel;
using OfficeOpenXml;

string path = Path.Combine(Environment.CurrentDirectory, @"Resources", "2020.rg.breakout.xlsx");
FileInfo fileInfo = new FileInfo(path);

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

Console.WriteLine("Uploading file to database");
Console.WriteLine("Window will close automatically when finished");

using (ExcelPackage excelPackage = new ExcelPackage(fileInfo))
{
    ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];

    Console.WriteLine("Adding Agency Categories");
    await CreateAgencyCategories(worksheet);
    Console.WriteLine("Adding Agencies");
    await CreateAgencies(worksheet);
    Console.WriteLine("Adding Relationships");
    await AddCategoriesToAgency(worksheet);
}
Console.WriteLine("Success");
System.Environment.Exit(0);

async Task AddCategoriesToAgency(ExcelWorksheet worksheet)
{
    using (PC2Context _context = new PC2Context())
    {
        List<AgencyCategory> categories = await AgencyCategoryDB.GetAgencyCategoriesAsync();
        List<Agency> agencies = await AgencyDB.GetAllAgencyAsync(_context);

        for (int row = Constants.StartingRow; row <= Constants.EndingRow; row++)
        {
            List<int> currentCategoryID = new List<int>();
            for (int col = Constants.CategoryStart; col <= Constants.CategoryEnd; col++)
            {
                if (worksheet.Cells[row, col].Value?.ToString() == "x")
                {
                    AgencyAgencyCategory agencyCategory = new AgencyAgencyCategory();
                    agencyCategory.AgenciesAgencyId = agencies[row - 2].AgencyId;
                    agencyCategory.AgencyCategoriesAgencyCategoryId = (col - Constants.CategoryStart + 1);
                    await AgencyDB.UpdateRelationships(agencyCategory);
                }
            }
        }
    }
}

/// <summary>
/// Creates the Categories for the agencies
/// </summary>
async Task CreateAgencyCategories(ExcelWorksheet worksheet)
{
    for (int col = Constants.CategoryStart; col <= Constants.CategoryEnd; col++)
    {
        AgencyCategory agencyCategory = new AgencyCategory();
        agencyCategory.AgencyCategoryName = worksheet.Cells[1, col].Value.ToString();
        await AgencyCategoryDB.AddCategoryAsync(agencyCategory);
    }
}

/// <summary>
/// Creates the agencies
/// </summary>
async Task CreateAgencies(ExcelWorksheet worksheet)
{
    for (int row = Constants.StartingRow; row <= Constants.EndingRow; row++)
    {
        Agency agency = new Agency();
        for (int col = 1; col < Constants.CategoryStart; col++)
        {
            switch (col)
            {
                case 1:
                    agency.AgencyName = worksheet.Cells[row, col].Value.ToString();
                    break;
                case 2:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.AgencyName2 = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 3:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Contact = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 4:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Address1 = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 5:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Address2 = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 6:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.City = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 7:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.State = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 8:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Zip = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 9:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.MailingAddress = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 10:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Phone = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 11:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Phone += " " + worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 12:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Phone += " " + worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 13:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Phone += " " + worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 14:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.TollFree = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 15:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.TollFree += " " + worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 16:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Tty = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 17:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Tty += " " + worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 18:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Tdd = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 19:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.CrisisHelpHotline = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 20:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Fax = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 21:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Fax += " " + worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 22:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Email = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 23:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Email += " " + worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 24:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Email += " " + worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 25:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Website = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 26:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Website += " " + worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
                case 27:
                    if (worksheet.Cells[row, col].Value != null)
                    {
                        agency.Description = worksheet.Cells[row, col].Value.ToString();
                    }
                    break;
            }
        }
        await AgencyDB.AddAgencyAsync(agency);
    }
}