using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ccd.Server.Users;

public class UserAddRequest
{
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required, MinLength(2), MaxLength(30)]
    public string FirstName { get; set; }

    [Required, MinLength(2), MaxLength(30)]
    public string LastName { get; set; }

    [Required] public Guid OrganizationId { get; set; }

    [Required] public string Role { get; set; }

    public List<string> Permissions { get; set; }
}
