namespace Ccd.Server.Authentication;

public class LoginInitResponse
{
    public string Action { get; set; } = "redirect";
    public string LoginHint { get; set; }
}
