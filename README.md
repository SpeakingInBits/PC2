# PC2
PC2 is a modern, database-driven website for the Pierce County Coalition for Developmental Disabilities (PC2). 
It provides a searchable resource guide, file uploads, and administrative features, replacing the original Wix-based site the client had.

## Technology Stack
- ASP.NET Core MVC (.NET 9)
- Azure Blob Storage (Azurite Emulator for local development)
- SQL Server (localdb for development)
- Visual Studio 2022

## Production Environment
This website is hosted on Azure App Service, providing a reliable and scalable platform for production use. The database 
is hosted on Azure SQL Database.

## Getting Started

### Prerequisites
- Visual Studio 2022
- Ensure the SQL Server Data Tools component are installed with Visual Studio.
- .NET 9 SDK (bundled with Visual Studio 2022)
- ASP.NET and web development workload

### Setup Instructions
1. Clone the repository.
2. Open the solution in Visual Studio.
3. Run `update-database` in the Package Manager Console for the `PC2` project.
4. Execute `PC2-TestData.sql` (found in the Solution Items folder) against localdb.
5. Run the website to create default roles and admin login.

### Azure Blob Storage
Azurite emulator is included as a dependency and runs automatically in Visual Studio. See [Azurite documentation](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio) for details.

## Admin Credentials
- Username: `admin@pc2online.org`
- Password: `Password01#`

## Contributors
Made possible by [contributors](https://github.com/SpeakingInBits/PC2/graphs/contributors) and students at Clover Park Technical College.

<a href="https://github.com/speakinginbits/pc2/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=speakinginbits/pc2" />
</a>

The success of this website is a direct result of the dedication and hard work of CPTC students. By collaborating on a real client project, 
students gain valuable hands-on experience in software development, teamwork, and project delivery. Their contributions not only enhance 
their technical skills, but also provide the rewarding opportunity to see their work deployed in a live production environment, making a 
meaningful impact for the community.

Made with [contrib.rocks](https://contrib.rocks).