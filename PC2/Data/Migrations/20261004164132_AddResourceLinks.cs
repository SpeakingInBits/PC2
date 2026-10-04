using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC2.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResourceLinks",
                columns: table => new
                {
                    ResourceLinkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceLinks", x => x.ResourceLinkId);
                });

            // Seed with the links that were previously hard-coded on the Resource Links page
            migrationBuilder.InsertData(
                table: "ResourceLinks",
                columns: new[] { "Name", "Url", "Description" },
                values: new object[,]
                {
                    { "Academic Accommodation Resources", "https://www.washington.edu/doit/programs", null },
                    { "ADD Association", "https://www.add.org", null },
                    { "Arc of the United States", "https://www.thearc.org", null },
                    { "Arc of Washington", "https://arcwa.org", null },
                    { "Autism Resources for Families", "https://www.nationalautismcenter.org/resources/for-families/", null },
                    { "Autism Society of America", "https://www.autism-society.org", null },
                    { "Autism Society of Washington", "https://www.autismsocietyofwa.org", null },
                    { "Benefits CheckUp", "https://www.benefitscheckup.org/", null },
                    { "CDC Autism Links & Resources", "https://www.cdc.gov/autism/", null },
                    { "Center for Parent Information and Resources", "https://www.parentcenterhub.org/", null },
                    { "Children's Defense Fund", "https://www.childrensdefense.org", null },
                    { "College for Students with Disabilities: A Guide for Students, Families, and Educators", "https://online.maryville.edu/disabilities-guide/", null },
                    { "Community Guide/Community Engagement Locator Map", "https://informingfamilies.org/locator", null },
                    { "Consortium for Citizens with Disabilities", "https://www.c-c-d.org", null },
                    { "Consumer Direct Care Network Washington", "https://www.consumerdirectwa.com/", null },
                    { "Developmental Disabilities Administration", "https://www.dshs.wa.gov/dda", null },
                    { "Digital Resources for Students with Autism", "https://teach.com/online-ed/psychology-degrees/online-masters-applied-behavior-analysis/aba-digital-autism-resources/", null },
                    { "Disability Rights Washington", "https://www.disabilityrightswa.org", null },
                    { "Easter Seals of Washington", "https://www.easterseals.com/", null },
                    { "Epilepsy Foundation of America", "https://www.epilepsy.com/", null },
                    { "Estate Planning for Parents of Kids with Autism", "https://www.justgreatlawyers.com/estate-planning-for-parents-of-children-with-autism", null },
                    { "Families for Effective Autism Treatment (FEAT) of Washington", "https://www.facebook.com/FEATofWA/", null },
                    { "Family Voices", "https://familyvoices.org", null },
                    { "Federation for Children with Special Needs", "https://www.fcsn.org", null },
                    { "Fragile X Foundation", "https://www.fragilex.org", null },
                    { "Gladnet - Glad Network Global Action on Disability", "https://gladnetwork.net/", null },
                    { "Inclusion Press", "https://www.inclusion.com", null },
                    { "Informing Families", "https://www.informingfamilies.org/", null },
                    { "Institute on Community Inclusion", "https://www.communityinclusion.org", null },
                    { "Institute on Community Integration", "https://www.ici.umn.edu/", null },
                    { "Institute on Independent Living", "https://www.independentliving.org", null },
                    { "Interactive Planner for Caregivers", "http://planner.thecplawyer.com/", null },
                    { "Kids Together", "https://www.kidstogether.org", null },
                    { "Learning Disabilities Association of Washington", "http://www.ldawa.org", null },
                    { "Medicare Resources", "https://medicare.com", "Operated by eHealthinsurance, serving more than 3 million customers since 2013. Partnerships with numerous Medicare insurance companies, and offering more than 5000 Medicare insurance plans online." },
                    { "My Life Plan - self-directed questionnaire for every stage of life", "https://mylifeplan.guide/guide/", null },
                    { "National Center for Learning Disabilities", "https://www.ncld.org", null },
                    { "National Council on Disability", "https://www.ncd.gov", null },
                    { "National Down Syndrome Society", "https://www.ndss.org", null },
                    { "National Organization on Disability", "https://www.nod.org", null },
                    { "National Rehabilitation Information Center (NARIC)", "https://www.naric.com", null },
                    { "New Mobility Magazine", "https://www.newmobility.com", null },
                    { "Nursing Home Abuse", "https://www.nursinghomeabuse.org/nursing-home-abuse/", null },
                    { "Operation Autism for Military Families", "https://operationautism.org/", null },
                    { "Parent Advocacy Coalition for Education Rights Center (PACER)", "https://www.pacer.org/", null },
                    { "People First of Washington", "https://www.peoplefirstofwashington.org/", null },
                    { "Pierce County Aging & Disability Resources", "https://www.co.pierce.wa.us/1986/Aging-Disability-Resources", null },
                    { "Pierce County Human Services, DD", "https://www.co.pierce.wa.us/4755/Developmental-Disabilities", null },
                    { "Reduce the Noise: Help Loved Ones with Sensory Overload Enjoy Shopping", "https://www.retailmenot.com/blog/sensory-overload-while-shopping.html", null },
                    { "Self Advocates in Leadership (SAIL)", "https://www.selfadvocatesinleadership.com/", null },
                    { "Sesame Street Autism Resources for Parents", "https://autism.sesamestreet.org/", null },
                    { "Society for Disability Studies", "https://www.disstudies.org", null },
                    { "Special Ed Advocate", "https://www.wrightslaw.com", null },
                    { "Tacoma Area Coalition of Individuals with Disabilities (TACID)", "https://tacid.org/", null },
                    { "TASH", "https://www.tash.org", null },
                    { "Through the Looking Glass", "https://www.lookingglass.org", null },
                    { "U.S. Department of Education", "https://www.ed.gov", null },
                    { "United Cerebral Palsy", "https://www.ucp.org", null },
                    { "Washington Assistive Technology Act Program", "https://watap.org", null },
                    { "Washington OSPI/Special Education", "https://www.k12.wa.us/specialed", null },
                    { "Washington PAVE", "https://wapave.org/", null },
                    { "Washington State Department of Health - Emergency Disability Resources", "https://www.doh.wa.gov/YouandYourFamily/DisabilityOrganizations", null },
                    { "Washington State Developmental Disabilities Council", "https://www.ddc.wa.gov/", null },
                    { "Washington State Father's Network", "https://fathersnetwork.org/", null },
                    { "Washington State Info Network (211)", "https://win211.org/", null },
                    { "Washington State Office of the DD Ombuds", "https://ddombuds.org/", null },
                    { "Washington State PTA", "https://www.wastatepta.org", null },
                    { "World Institute on Disability", "https://www.wid.org", null },
                    { "Yellow Pages for Kids with Disabilities", "https://www.yellowpagesforkids.com", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceLinks");
        }
    }
}
