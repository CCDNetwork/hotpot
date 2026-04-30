using System.ComponentModel.DataAnnotations;

namespace Ccd.Server.Authentication;

public class LoginInitRequest
{
    [Required, EmailAddress]
    public string Email { get; set; }
}
