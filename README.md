# DeskFlow

DeskFlow is a learning and portfolio project for managing IT support requests from submission through assignment, investigation, resolution, and closure.

The project is being built progressively with ASP.NET Core MVC, C#, Entity Framework Core, and PostgreSQL. Its interface is designed to feel calm and practical: clear ticket queues, restrained status colors, useful empty states, and role-specific navigation. The selected framework target is **.NET 10 LTS**.

## Project status

**Phase 1 – Project foundation** is in progress. The architecture and database design are documented before the application schema is implemented.

## Planned capabilities

- Role-based access for administrators, helpdesk managers, technicians, and end users
- Ticket submission, assignment, status workflow, comments, and history
- Private attachments and in-application notifications
- Configurable response and resolution targets
- Operational dashboards, reports, and a REST API

## Technology

- ASP.NET Core MVC and C# on .NET 8
- Entity Framework Core with Npgsql
- PostgreSQL, with Supabase planned for hosted database and file storage
- ASP.NET Core Identity
- Bootstrap 5, JavaScript, and Chart.js
- xUnit for automated tests

## Development approach

DeskFlow is being developed in small, verifiable phases. The database model and relationships will be documented before migrations are introduced. Secrets such as database connection strings and Supabase service keys must stay outside source control.

## Design documents

- [System architecture and development roadmap](docs/architecture-and-roadmap.md)
- [Database entity relationship diagram](docs/database-erd.md)

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
