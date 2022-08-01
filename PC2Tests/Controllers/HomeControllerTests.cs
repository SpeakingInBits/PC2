using IdentityLogin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PC2.Controllers;
using PC2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PC2.Controllers.Tests
{
    [TestClass()]
    public class HomeControllerTests
    {
        [TestMethod()]
        public void Index_ReturnsViewResult()
        {
            // Arrange
            Mock<ILogger<HomeController>> mockLogger = new();
            Mock<IEmailSender> mockEmailSender = new();

            DbContextOptionsBuilder<ApplicationDbContext> dbOptions = new();
            dbOptions.UseInMemoryDatabase("PC2");
            
            ApplicationDbContext db = new(dbOptions.Options);
            
            
            HomeController controller = new(mockLogger.Object, db, mockEmailSender.Object);

            // Act
            ViewResult? result = controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.ViewName); // The default view is returned
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }
    }
}