
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using var context = new EventHubContext();
context.Database.EnsureCreated();

var organizer = new Organizer
{
    Name = "Ahmed",
    CompanyName = "Tech Co",
    IsVerified = true,
    Profile = new OrganizerProfile { Biography = "Event pro", WebsiteUrl = "https://tech.co" }
};
organizer.Events.Add(new Event
{
    Title = "C# Meetup",
    Description = "Intro to EF Core",
    StartDate = DateTime.Now.AddDays(10),
    MaxAttendees = 50
});

var attendee = new Attendee
{
    FullName = "Sara Ali",
    Email = "sara@mail.com",
    Street = "10 Nile St",
    City = "Cairo",
    Country = "Egypt",
    PostalCode = "11511",
    Badge = new Badge { BadgeNumber = 1001, IssuedDate = DateTime.Now, Tier = BadgeTier.VIP }
};

context.Add(organizer);
context.Add(attendee);
context.SaveChanges();

Console.WriteLine(attendee.FullAddress);

// 4, 5, 8
[Table("EventOrganizers")]
public class Organizer
{
    public int Id { get; set; }

    // 6, 7
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    // 6
    [MaxLength(150)]
    public string? CompanyName { get; set; }

    public bool IsVerified { get; set; }

    public OrganizerProfile? Profile { get; set; }
    public ICollection<Event> Events { get; set; } = new List<Event>();
}

// 4, 5
public class Attendee
{
    public int Id { get; set; }

    // 6, 7
    [Required, MaxLength(150)]
    public string FullName { get; set; } = null!;

    // 6, 7
    [Required, MaxLength(200)]
    public string Email { get; set; } = null!;

    // 6, 7
    [Required, MaxLength(150)]
    public string Street { get; set; } = null!;

    // 6, 7
    [Required, MaxLength(100)]
    public string City { get; set; } = null!;

    // 6, 7
    [Required, MaxLength(100)]
    public string Country { get; set; } = null!;

    // 6, 7, 9
    [Required, MaxLength(20)]
    [Column("ZipCode")]
    public string PostalCode { get; set; } = null!;

    // 10
    [NotMapped]
    public string FullAddress => $"{Street}, {City}, {Country}, {PostalCode}";

    public Badge? Badge { get; set; }
}

public class OrganizerProfile
{
    public int Id { get; set; }
    public string? Biography { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }

    public int OrganizerId { get; set; }
    public Organizer Organizer { get; set; } = null!;
}

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int MaxAttendees { get; set; }

    public int OrganizerId { get; set; }
    public Organizer Organizer { get; set; } = null!;
}

public enum BadgeTier { Standard, VIP }

public class Badge
{
    public int Id { get; set; }
    public int BadgeNumber { get; set; }
    public DateTime IssuedDate { get; set; }
    public BadgeTier Tier { get; set; }

    public int AttendeeId { get; set; }
    public Attendee Attendee { get; set; } = null!;
}

// 4, 11 (separate configuration class)
public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.HasKey(b => b.Id);

        builder.HasIndex(b => b.BadgeNumber).IsUnique();

        builder.Property(b => b.IssuedDate).HasDefaultValueSql("GETDATE()");

        builder.Property(b => b.Tier)
               .HasConversion<string>()
               .HasMaxLength(20);

        // 3
        builder.HasOne(b => b.Attendee)
               .WithOne(a => a.Badge)
               .HasForeignKey<Badge>(b => b.AttendeeId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EventHubContext : DbContext
{
    public DbSet<Organizer> Organizers => Set<Organizer>();
    public DbSet<OrganizerProfile> OrganizerProfiles => Set<OrganizerProfile>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Attendee> Attendees => Set<Attendee>();
    public DbSet<Badge> Badges => Set<Badge>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            "Server=.;Database=EventHubDb;Trusted_Connection=True;TrustServerCertificate=True;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 4, 11 (directly in OnModelCreating)
        modelBuilder.Entity<OrganizerProfile>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Biography).HasMaxLength(1000);
            entity.Property(p => p.WebsiteUrl).HasMaxLength(300);
            entity.Property(p => p.LogoUrl).HasMaxLength(300);

            // 1
            entity.HasOne(p => p.Organizer)
                  .WithOne(o => o.Profile)
                  .HasForeignKey<OrganizerProfile>(p => p.OrganizerId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 4, 11 (directly in OnModelCreating)
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired(false);
            entity.Property(e => e.MaxAttendees).IsRequired();

            // 2
            entity.HasOne(e => e.Organizer)
                  .WithMany(o => o.Events)
                  .HasForeignKey(e => e.OrganizerId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 11 (apply the separate configuration class)
        modelBuilder.ApplyConfiguration(new BadgeConfiguration());
    }
}
