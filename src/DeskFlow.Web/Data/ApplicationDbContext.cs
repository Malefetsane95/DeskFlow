using DeskFlow.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    public DbSet<TicketPriority> TicketPriorities => Set<TicketPriority>();
    public DbSet<TicketStatus> TicketStatuses => Set<TicketStatus>();
    public DbSet<TicketAssignment> TicketAssignments => Set<TicketAssignment>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketHistory> TicketHistory => Set<TicketHistory>();
    public DbSet<SlaRule> SlaRules => Set<SlaRule>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserFeedback> UserFeedback => Set<UserFeedback>();
    public DbSet<TicketReferenceCounter> TicketReferenceCounters => Set<TicketReferenceCounter>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(profile => profile.UserId);
            entity.HasOne(profile => profile.User)
                .WithOne(user => user.Profile)
                .HasForeignKey<UserProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TicketCategory>(entity =>
        {
            entity.HasIndex(category => category.Name).IsUnique();
            entity.Property(category => category.Name).IsRequired();
            entity.HasData(
                new TicketCategory { Id = 1, Name = "Hardware Problems", Description = "Computers and physical equipment", IsActive = true },
                new TicketCategory { Id = 2, Name = "Software Problems", Description = "Applications and operating systems", IsActive = true },
                new TicketCategory { Id = 3, Name = "Network and Connectivity", Description = "Network access and connectivity", IsActive = true },
                new TicketCategory { Id = 4, Name = "Printers and Peripherals", Description = "Printers and connected devices", IsActive = true },
                new TicketCategory { Id = 5, Name = "Accounts and Access", Description = "Accounts, permissions, and sign-in", IsActive = true },
                new TicketCategory { Id = 6, Name = "Email", Description = "Email and calendar support", IsActive = true },
                new TicketCategory { Id = 7, Name = "Security Incidents", Description = "Potential security concerns", IsActive = true },
                new TicketCategory { Id = 8, Name = "Other IT Support", Description = "Requests not covered by another category", IsActive = true });
        });

        builder.Entity<TicketPriority>(entity =>
        {
            entity.HasIndex(priority => priority.Code).IsUnique();
            entity.HasIndex(priority => priority.Name).IsUnique();
            entity.HasData(
                new TicketPriority { Id = 1, Code = "LOW", Name = "Low", Color = "#667085", SortOrder = 1, IsActive = true },
                new TicketPriority { Id = 2, Code = "MEDIUM", Name = "Medium", Color = "#2563A6", SortOrder = 2, IsActive = true },
                new TicketPriority { Id = 3, Code = "HIGH", Name = "High", Color = "#A15C07", SortOrder = 3, IsActive = true },
                new TicketPriority { Id = 4, Code = "CRITICAL", Name = "Critical", Color = "#B42318", SortOrder = 4, IsActive = true });
        });

        builder.Entity<TicketStatus>(entity =>
        {
            entity.HasIndex(status => status.Code).IsUnique();
            entity.HasIndex(status => status.Name).IsUnique();
            entity.HasData(
                new TicketStatus { Id = 1, Code = "OPEN", Name = "Open", SortOrder = 1, IsFinal = false },
                new TicketStatus { Id = 2, Code = "ASSIGNED", Name = "Assigned", SortOrder = 2, IsFinal = false },
                new TicketStatus { Id = 3, Code = "IN_PROGRESS", Name = "In Progress", SortOrder = 3, IsFinal = false },
                new TicketStatus { Id = 4, Code = "ON_HOLD", Name = "On Hold", SortOrder = 4, IsFinal = false },
                new TicketStatus { Id = 5, Code = "RESOLVED", Name = "Resolved", SortOrder = 5, IsFinal = false },
                new TicketStatus { Id = 6, Code = "CLOSED", Name = "Closed", SortOrder = 6, IsFinal = true },
                new TicketStatus { Id = 7, Code = "REOPENED", Name = "Reopened", SortOrder = 7, IsFinal = false });
        });

        builder.Entity<Ticket>(entity =>
        {
            entity.HasIndex(ticket => ticket.Reference).IsUnique();
            entity.HasIndex(ticket => new { ticket.RequesterId, ticket.CreatedAt });
            entity.HasIndex(ticket => new { ticket.StatusId, ticket.PriorityId, ticket.CreatedAt });

            entity.HasOne(ticket => ticket.Requester)
                .WithMany(user => user.RequestedTickets)
                .HasForeignKey(ticket => ticket.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(ticket => ticket.Category)
                .WithMany(category => category.Tickets)
                .HasForeignKey(ticket => ticket.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(ticket => ticket.Priority)
                .WithMany(priority => priority.Tickets)
                .HasForeignKey(ticket => ticket.PriorityId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(ticket => ticket.Status)
                .WithMany(status => status.Tickets)
                .HasForeignKey(ticket => ticket.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TicketAssignment>(entity =>
        {
            entity.HasIndex(assignment => new { assignment.TechnicianId, assignment.UnassignedAt });
            entity.HasIndex(assignment => new { assignment.TicketId, assignment.AssignedAt });
            entity.HasIndex(assignment => assignment.TicketId)
                .IsUnique()
                .HasFilter("\"UnassignedAt\" IS NULL")
                .HasDatabaseName("IX_TicketAssignments_OneActiveAssignmentPerTicket");

            entity.HasOne(assignment => assignment.Ticket)
                .WithMany(ticket => ticket.Assignments)
                .HasForeignKey(assignment => assignment.TicketId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(assignment => assignment.Technician)
                .WithMany(user => user.TechnicianAssignments)
                .HasForeignKey(assignment => assignment.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(assignment => assignment.AssignedBy)
                .WithMany(user => user.AssignmentsMade)
                .HasForeignKey(assignment => assignment.AssignedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TicketComment>(entity =>
        {
            entity.Property(comment => comment.Visibility)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.HasIndex(comment => new { comment.TicketId, comment.CreatedAt });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_TicketComments_Visibility",
                "\"Visibility\" IN ('Public', 'Internal')"));

            entity.HasOne(comment => comment.Ticket)
                .WithMany(ticket => ticket.Comments)
                .HasForeignKey(comment => comment.TicketId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(comment => comment.Author)
                .WithMany(user => user.Comments)
                .HasForeignKey(comment => comment.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TicketAttachment>(entity =>
        {
            entity.HasIndex(attachment => attachment.ObjectKey).IsUnique();
            entity.HasIndex(attachment => new { attachment.TicketId, attachment.CreatedAt });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_TicketAttachments_SizeBytes_NonNegative",
                "\"SizeBytes\" >= 0"));
            entity.HasOne(attachment => attachment.Ticket)
                .WithMany(ticket => ticket.Attachments)
                .HasForeignKey(attachment => attachment.TicketId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(attachment => attachment.UploadedBy)
                .WithMany(user => user.UploadedAttachments)
                .HasForeignKey(attachment => attachment.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TicketHistory>(entity =>
        {
            entity.HasIndex(history => new { history.TicketId, history.OccurredAt });
            entity.HasOne(history => history.Ticket)
                .WithMany(ticket => ticket.History)
                .HasForeignKey(history => history.TicketId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(history => history.Actor)
                .WithMany()
                .HasForeignKey(history => history.ActorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SlaRule>(entity =>
        {
            entity.HasIndex(rule => rule.PriorityId).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_SlaRules_ResponseTarget_Positive", "\"ResponseTargetMinutes\" > 0");
                table.HasCheckConstraint("CK_SlaRules_ResolutionTarget_Positive", "\"ResolutionTargetMinutes\" > 0");
            });
            entity.HasOne(rule => rule.Priority)
                .WithOne(priority => priority.SlaRule)
                .HasForeignKey<SlaRule>(rule => rule.PriorityId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasData(
                new SlaRule { Id = 1, PriorityId = 4, ResponseTargetMinutes = 15, ResolutionTargetMinutes = 240, IsActive = true },
                new SlaRule { Id = 2, PriorityId = 3, ResponseTargetMinutes = 60, ResolutionTargetMinutes = 480, IsActive = true },
                new SlaRule { Id = 3, PriorityId = 2, ResponseTargetMinutes = 240, ResolutionTargetMinutes = 1440, IsActive = true },
                new SlaRule { Id = 4, PriorityId = 1, ResponseTargetMinutes = 480, ResolutionTargetMinutes = 2880, IsActive = true });
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasIndex(notification => new { notification.RecipientId, notification.ReadAt, notification.CreatedAt });
            entity.HasOne(notification => notification.Recipient)
                .WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(notification => notification.Ticket)
                .WithMany(ticket => ticket.Notifications)
                .HasForeignKey(notification => notification.TicketId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.Property(log => log.PreviousValueJson).HasColumnType("jsonb");
            entity.Property(log => log.NewValueJson).HasColumnType("jsonb");
            entity.HasIndex(log => new { log.ActorId, log.OccurredAt });
            entity.HasIndex(log => new { log.TicketId, log.OccurredAt });
            entity.HasOne(log => log.Actor)
                .WithMany()
                .HasForeignKey(log => log.ActorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(log => log.Ticket)
                .WithMany()
                .HasForeignKey(log => log.TicketId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserFeedback>(entity =>
        {
            entity.HasIndex(feedback => feedback.TicketId).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_UserFeedback_Rating_Range",
                "\"Rating\" BETWEEN 1 AND 5"));
            entity.HasOne(feedback => feedback.Ticket)
                .WithOne(ticket => ticket.Feedback)
                .HasForeignKey<UserFeedback>(feedback => feedback.TicketId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(feedback => feedback.User)
                .WithMany()
                .HasForeignKey(feedback => feedback.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TicketReferenceCounter>(entity =>
        {
            entity.HasKey(counter => counter.ReferenceYear);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_TicketReferenceCounters_LastNumber_NonNegative",
                "\"LastNumber\" >= 0"));
        });
    }
}
