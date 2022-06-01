# PC2
Custom replacement website for https://pc2online.org. The goal of this project is to make the current website more dynamic and include client requested features. The main feature is a
database driven, searchable resource guide to replace/complement the physical book they print every year.

## Getting started
- [VS2022](https://visualstudio.microsoft.com/)
- ASP.NET and web development workload
- .NET 6 (Included in VS2022)

## Create the database and insert test data
- Run ```update-database``` in the ```PC2``` project. 
- Open the ```PC2-TestData.sql``` script in the ```Solution Items``` folder. Execute it against localdb
- Run the website to create the default roles and admin login
