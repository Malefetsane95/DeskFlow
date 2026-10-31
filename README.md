# DeskFlow

DeskFlow is a learning and portfolio project for managing IT support requests from submission through assignment, investigation, resolution, and closure.

The project is being built progressively with ASP.NET Core MVC, C#, Entity Framework Core, and PostgreSQL. Its interface is designed to feel calm and practical: clear ticket queues, restrained status colors, useful empty states, and role-specific navigation. The selected framework target is **.NET 10 LTS**.

## Project status

**The core requester and staff ticket workflows are in place.** Users can register, sign in, submit requests, track progress, and exchange public replies with the IT team. Staff can manage the shared queue, assign work, update statuses, and add internal notes. See the completed [staff queue issue #1](https://github.com/Malefetsane95/DeskFlow/issues/1) and the conversation work in [issue #2](https://github.com/Malefetsane95/DeskFlow/issues/2).

## Planned capabilities

- Role-based access for administrators, helpdesk managers, technicians, and end users
- Ticket submission, assignment, status workflow, comments, and history
- Private attachments and in-application notifications
- Configurable response and resolution targets
- Operational dashboards, reports, and a REST API

## Technology

- ASP.NET Core MVC and C# on .NET 10 LTS
- Entity Framework Core with Npgsql
- PostgreSQL, with Supabase planned for hosted database and file storage
- ASP.NET Core Identity
- Bootstrap 5, JavaScript, and Chart.js
- xUnit for automated tests

## Development approach

DeskFlow is being developed in small, verifiable phases. The database model and relationships will be documented before migrations are introduced. Secrets such as database connection strings and Supabase service keys must stay outside source control.

## Build the starter application

From the repository root:

```powershell
dotnet tool restore
dotnet restore DeskFlow.sln
dotnet build DeskFlow.sln
```

The MVC shell can run locally without a database connection. Identity and database-backed features become available after `ConnectionStrings:DefaultConnection` is set in User Secrets.

To set the database connection locally, replace the example values with the host, port, and username from Supabase's **Connect** panel, then run this command in your terminal. Keep the actual connection string private:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<host>;Port=5432;Database=postgres;Username=<username>;Password=<database-password>;SSL Mode=Require" --project src/DeskFlow.Web
```

Apply the schema and start the web app after the secret is set:

```powershell
dotnet tool run dotnet-ef database update --project src/DeskFlow.Web --startup-project src/DeskFlow.Web
dotnet run --project src/DeskFlow.Web
```

## Design documents

- [System architecture and development roadmap](architecture-and-roadmap.md)
- [Database entity relationship diagram](database-erd.md)

## Planned phases

1. Project foundation and database design
2. Authentication, roles, and profiles
3. Shared layout and role-specific dashboards
4. Ticket creation and management
5. Assignment, communication, and ticket history
6. Attachments and notifications
7. SLA tracking, dashboards, and reporting
8. REST API and automated tests
9. Deployment and portfolio documentation

## License

No license has been selected yet.
