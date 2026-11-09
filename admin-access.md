# DeskFlow administrator and team access

## Open the team tools

After signing in with the right role, use **Overview** and **Team queue** in the top navigation. These pages are available to IT Technicians and Helpdesk Managers. Helpdesk Managers also see **SLA targets**.

Administrators see **Team access**. There they can find registered accounts and grant or remove the IT Technician and Helpdesk Manager roles. Existing End User roles are preserved. A role change appears after that account signs in again.

## Set up the first Administrator

Public registration creates End User accounts only. The app seeds the four role records, but it does not automatically make any account an Administrator. Assign the first Administrator once through the Supabase SQL Editor after the application has connected to the database and created the Identity roles.

1. Register the account that should administer DeskFlow, then sign out.
2. In Supabase, open **SQL Editor** for the DeskFlow project.
3. Replace `admin@example.com` below with that account's email address and run the statement.

```sql
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "AspNetUsers" AS u
JOIN "AspNetRoles" AS r ON r."Name" = 'Administrator'
WHERE lower(u."Email") = lower('admin@example.com')
  AND NOT EXISTS (
      SELECT 1
      FROM "AspNetUserRoles" AS existing
      WHERE existing."UserId" = u."Id"
        AND existing."RoleId" = r."Id"
  );
```

The statement inserts a role assignment only when the account and role exist, and it is safe to run again. If it inserts no row, confirm the user registered and the app has seeded Identity roles.

4. Sign back in with that account and open **Team access** from the navigation.
5. Find a team member by display name or email, then grant **IT Technician** or **Helpdesk Manager** access.

The Team access page cannot edit Administrator membership, so it cannot demote or remove the last Administrator. Use the SQL procedure only for the initial administrator setup; use the application page for routine team roles.
