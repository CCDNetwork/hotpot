using System;
using System.ComponentModel.DataAnnotations;
using Ccd.Server.Data;
using Ccd.Server.Helpers;

namespace Ccd.Server.Users;

public class User : IHasPassword, IIsDeleted
{
    public static readonly User SYSTEM_USER = new User
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        Email = "admin@ccd.org",
        Password = "",
        FirstName = "System",
        LastName = "",
        ActivatedAt = new DateTime(2022, 01, 01, 0, 0, 0, DateTimeKind.Utc),
        CreatedAt = new DateTime(2022, 01, 01, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2022, 01, 01, 0, 0, 0, DateTimeKind.Utc)
    };

    public Guid Id { get; set; } = IdProvider.NewId();

    [EmailAddress, QuickSearchable]
    public string Email { get; set; }
    public string Password { get; set; }

    [Required, QuickSearchable]
    public string FirstName { get; set; }

    [QuickSearchable]
    public string LastName { get; set; }
    public string ActivationCode { get; set; }
    public string PasswordResetCode { get; set; }
    public DateTime? PasswordResetCodeExpiresAt { get; set; }

    public string Language { get; set; } = "en";
    public DateTime? ActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
