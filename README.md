# PC2
Custom replacement website for https://pc2online.org. The goal of this project is to make the current website more dynamic and include client requested features. The main feature is a
database driven, searchable resource guide to replace/complement the physical book they print every year.

## Local Development Setup

## Getting started
- [VS2022](https://visualstudio.microsoft.com/)
- ASP.NET and web development workload
- .NET 9 (Included in VS2022)

### Create the database and insert test data
- Run ```update-database``` in the ```PC2``` project. 
- Open the ```PC2-TestData.sql``` script in the ```Solution Items``` folder. Execute it against localdb
- Run the website to create the default roles and admin login

### Azure Blob Storage (Azurite Emulator)

This project uses Azure Blob Storage for file uploads. For local development, you can use the Azurite emulator. It
is already added to the PC2 project as a dependency, so it should run directly from Visual Studio automatically when you run the project.
See the [Azurite documentation](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio) for more details.

## Admin Credentials
- To log in as admin on the dev site, the username/email is `admin@pc2online.org` and the password is Password01#

## Contributors

Made possible with help by [contributors](https://github.com/SpeakingInBits/PC2/graphs/contributors) and collaboration with students at CPTC:

<a href="https://github.com/speakinginbits/pc2/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=speakinginbits/pc2" />
</a>

Made with [contrib.rocks](https://contrib.rocks).
