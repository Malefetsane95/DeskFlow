# DeskFlow

DeskFlow is a learning and portfolio project for managing IT support requests from submission through assignment, investigation, resolution, and closure.

The project is being built progressively with ASP.NET Core MVC, C#, Entity Framework Core, and PostgreSQL. Its interface is designed to feel calm and practical: clear ticket queues, restrained status colors, useful empty states, and role-specific navigation. The selected framework target is **.NET 10 LTS**.

## Project status

**Requester and staff ticket workflows are in place.** Users can register, submit requests, track progress, exchange public replies, attach private files, and receive in-app updates. Staff can manage and filter the shared queue, review an actionable team overview, assign work, update statuses, add internal notes, set SLA targets, access files, export filtered queue results to CSV, and view ticket activity reports. Administrators can manage Technician and Helpdesk Manager access. The requester API provides paginated ticket lists and details with public replies only. See completed [staff queue issue #1](https://github.com/Malefetsane95/DeskFlow/issues/1), [conversation issue #2](https://github.com/Malefetsane95/DeskFlow/issues/2), [notification issue #3](https://github.com/Malefetsane95/DeskFlow/issues/3), [private attachments issue #4](https://github.com/Malefetsane95/DeskFlow/issues/4), [queue views issue #5](https://github.com/Malefetsane95/DeskFlow/issues/5), [SLA progress issue #6](https://github.com/Malefetsane95/DeskFlow/issues/6), [SLA settings issue #7](https://github.com/Malefetsane95/DeskFlow/issues/7), [staff overview issue #9](https://github.com/Malefetsane95/DeskFlow/issues/9), [queue CSV issue #11](https://github.com/Malefetsane95/DeskFlow/issues/11), [requester API issue #12](https://github.com/Malefetsane95/DeskFlow/issues/12), [admin team access issue #13](https://github.com/Malefetsane95/DeskFlow/issues/13), [workspace design issue #14](https://github.com/Malefetsane95/DeskFlow/issues/14), and [ticket activity report issue #15](https://github.com/Malefetsane95/DeskFlow/issues/15).

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

The MVC shell can run locally without a database connection. Identity and database-backed features become available after `ConnectionStrings:DefaultConnection` is set in User Secrets. File uploads also require the private `ticket-attachments` bucket in the Supabase Storage dashboard and a server-side service role key. Never put the service role key in source control or share it in chat.

To set the database connection locally, replace the example values with the host, port, and username from Supabase's **Connect** panel, then run this command in your terminal. Keep the actual connection string private:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<host>;Port=5432;Database=postgres;Username=<username>;Password=<database-password>;SSL Mode=Require" --project src/DeskFlow.Web
```

Apply the schema and start the web app after the secret is set:

```powershell
dotnet tool run dotnet-ef database update --project src/DeskFlow.Web --startup-project src/DeskFlow.Web
dotnet run --project src/DeskFlow.Web
```

Set the Storage service role key locally using the key from your Supabase project's API settings. The project URL and bucket name are already configured in `appsettings.json`:

```powershell
dotnet user-secrets set "Supabase:SecretKey" "<your-supabase-secret-key>" --project src/DeskFlow.Web
```

In Supabase Storage, create a bucket named `ticket-attachments` and leave **Public bucket** turned off. DeskFlow checks the ticket's requester or staff role before issuing a five-minute download link. Uploads allow PNG, JPG, PDF, TXT, and LOG files up to 10 MB each, with five files per ticket.

Helpdesk Managers can set first-response and resolution targets from 15 minutes to 30 calendar days in 15-minute increments at **SLA targets**. Changes apply immediately to staff SLA progress indicators.

## Design documents

- [System architecture and development roadmap](architecture-and-roadmap.md)
- [Database entity relationship diagram](database-erd.md)
- [Requester ticket API](requester-api.md)
- [Administrator and team access setup](admin-access.md)

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
