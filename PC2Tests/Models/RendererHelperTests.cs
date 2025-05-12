using Microsoft.VisualStudio.TestTools.UnitTesting;
using PC2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PC2.Models;

namespace PC2.Models.Tests
{
    [TestClass()]
    public class RendererHelperTests
    {
        [DataTestMethod]
        [DataRow(
            "Call us at (253) 564-0707 or 253-555-1234 for info.",
            "Call us at <a href=\"tel:2535640707\">(253) 564-0707</a><br> or <a href=\"tel:2535551234\">253-555-1234</a><br> for info."
        )]
        [DataRow(
            "Main: 253.111.2222, Alt: (425) 333-4444.",
            "Main: <a href=\"tel:2531112222\">253.111.2222</a><br>, Alt: <a href=\"tel:4253334444\">(425) 333-4444</a><br>."
        )]
        [DataRow(
            "No phone here.",
            "No phone here."
        )]
        [DataRow(
            "",
            ""
        )]
        public void RenderPhoneTest_phoneNumbersShouldBeWrappedInAnchor(string input, string expected)
        {
            // Act
            string result = RendererHelper.RenderPhone(input);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}


