using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ccd.Server.AdministrativeRegions;
using Ccd.Server.Beneficiaries;
using Ccd.Server.BeneficiaryAttributes;
using Ccd.Server.Deduplication;
using Ccd.Server.Handbooks;
using Ccd.Server.Organizations;
using Ccd.Server.Referrals;
using Ccd.Server.Storage;
using Ccd.Server.Templates;
using Ccd.Server.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ccd.Server.Data;

public class CcdContext : DbContext
{
    public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
    private readonly DbUserTrackingService _dbUserTrackingService;

    public CcdContext(
        DbContextOptions<CcdContext> options,
        DbUserTrackingService dbUserTrackingService
    )
        : base(options)
    {
        _dbUserTrackingService = dbUserTrackingService;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<File> Files { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<UserOrganization> UserOrganizations { get; set; }
    public DbSet<Beneficary> Beneficaries { get; set; }
    public DbSet<BeneficaryDeduplication> BeneficaryDeduplications { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingLog> BookingLogs { get; set; }
    public DbSet<BeneficiaryAttribute> BeneficiaryAttributes { get; set; }
    public DbSet<List> Lists { get; set; }
    public DbSet<Referral> Referrals { get; set; }
    public DbSet<Discussion> Discussions { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<Settings.Settings> Settings { get; set; }
    public DbSet<AdministrativeRegion> AdministrativeRegions { get; set; }
    public DbSet<Handbook> Handbooks { get; set; }
    public DbSet<BeneficiaryAttributeGroup> BeneficiaryAttributeGroups { get; set; }
    public DbSet<BaBag> BaBags { get; set; }

    private void seedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(User.SYSTEM_USER);

        modelBuilder.Entity<Settings.Settings>().HasData(Server.Settings.Settings.DEFAULT_SETTINGS);

        modelBuilder.Entity<BeneficiaryAttribute>().HasData(
            new BeneficiaryAttribute
            {
                Id = 1,
                Name = "First Name",
                AttributeName = "FirstName",
                Type = "string",
                UsedForDeduplication = true
            },
            new BeneficiaryAttribute
            {
                Id = 2,
                Name = "Family Name",
                AttributeName = "FamilyName",
                Type = "string",
                UsedForDeduplication = true
            },
            new BeneficiaryAttribute
            { Id = 3, Name = "Gender", AttributeName = "Gender", Type = "string", UsedForDeduplication = false },
            new BeneficiaryAttribute
            {
                Id = 4,
                Name = "Date of Birth",
                AttributeName = "DateOfBirth",
                Type = "DateTime",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            { Id = 5, Name = "HH ID", AttributeName = "HhId", Type = "string", UsedForDeduplication = false },
            new BeneficiaryAttribute
            {
                Id = 6,
                Name = "Mobile Phone ID",
                AttributeName = "MobilePhoneId",
                Type = "int",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 7,
                Name = "Gov ID Type",
                AttributeName = "GovIdType",
                Type = "string",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 8,
                Name = "Gov ID Number",
                AttributeName = "GovIdNumber",
                Type = "string",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 9,
                Name = "Other ID Type",
                AttributeName = "OtherIdType",
                Type = "string",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 10,
                Name = "Other ID Number",
                AttributeName = "OtherIdNumber",
                Type = "string",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 11,
                Name = "Assistance Details",
                AttributeName = "AssistanceDetails",
                Type = "string",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 12,
                Name = "Activity",
                AttributeName = "Activity",
                Type = "string",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 13,
                Name = "Currency",
                AttributeName = "Currency",
                Type = "string",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 14,
                Name = "Currency Amount",
                AttributeName = "CurrencyAmount",
                Type = "int",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 15,
                Name = "Start Date",
                AttributeName = "StartDate",
                Type = "DateTime",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 16,
                Name = "End Date",
                AttributeName = "EndDate",
                Type = "DateTime",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 17,
                Name = "Frequency",
                AttributeName = "Frequency",
                Type = "string",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 18,
                Name = "AdminLevel1",
                AttributeName = "AdminLevel1",
                Type = "string",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 19,
                Name = "AdminLevel2",
                AttributeName = "AdminLevel2",
                Type = "string",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 20,
                Name = "AdminLevel3",
                AttributeName = "AdminLevel3",
                Type = "string",
                UsedForDeduplication = false
            },
            new BeneficiaryAttribute
            {
                Id = 21,
                Name = "AdminLevel4",
                AttributeName = "AdminLevel4",
                Type = "string",
                UsedForDeduplication = false
            }
        );
    }

    private void disableCascadeDeletes(ModelBuilder modelBuilder)
    {
        var cascadeFKs = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(t => t.GetForeignKeys())
            .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

        foreach (var fk in cascadeFKs)
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        DbFormatter.SetDefaultValues(modelBuilder);
        DbFormatter.FormatTableNames(modelBuilder);
        DbFormatter.FormatColumnsSnakeCase(modelBuilder);

        disableCascadeDeletes(modelBuilder);
        seedData(modelBuilder);
    }

    public override int SaveChanges()
    {
        SoftDelete.ProcessSoftDeletedItems(ChangeTracker);

        // this will be null while seeding data and creating initial organization users
        if (_dbUserTrackingService != null)
            UserChangeTracker.ProcessUserChangeTrackedItems(
                ChangeTracker,
                _dbUserTrackingService.GetCurrentUserId(User.SYSTEM_USER.Id)
            );

        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    )
    {
        SoftDelete.ProcessSoftDeletedItems(ChangeTracker);

        // this will be null while seeding data and creating initial organization users
        if (_dbUserTrackingService != null)
            UserChangeTracker.ProcessUserChangeTrackedItems(
                ChangeTracker,
                _dbUserTrackingService.GetCurrentUserId(User.SYSTEM_USER.Id)
            );

        return base.SaveChangesAsync(cancellationToken);
    }
}