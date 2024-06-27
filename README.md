# PassionProject

The Real Estate application is a simple minimalistic CMS for posting & managing property listings.

## Core Features

- **Onboarding**
- **Authentication**
- **Agent management of listings**
- **Public view of listings with filter functionalities**

## Technologies

- **ASP.NET MVC**: Uses database, web API, MVC, HTTP Client simulation, and LINQ to perform CRUD operations.
- **Bootstrap**: Frontend framework
- **Database**: SQL Server
- **Razor Pages**: Views

## Getting Started

To set up and run the application locally, follow these steps:

1. Clone the repository to your local machine.
2. Open the solution file (`*.sln`) in [Visual Studio](./PassionProject.sln).
3. Ensure there is an `App_Data` folder in the project (Right click solution > View in File Explorer).
4. Open the Package Manager Console (Tools > NuGet Package Manager > Package Manager Console) and run the `Update-Database` command.
5. Check that the database is created using (View > SQL Server Object Explorer > MSSQLLocalDb > ...).
6. Run the application using the built-in development server.
