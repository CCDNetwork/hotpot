namespace Ccd.Server.Users;

public class UserRole
{
    public const string Admin = "admin";
    public const string UserAdmin = "user-admin";
    public const string OrganizationAdmin = "organization-admin";
    public const string User = "user";
    public const string SuperAdmin = "superadmin";

    public static bool IsValidRole(string modelRole)
    {
        return modelRole is Admin or UserAdmin or OrganizationAdmin or User;
    }

    /// <summary>
    ///     Only admins can grant the admin role. Keeping an existing admin as admin is always allowed,
    ///     so non-admins can still edit other fields of an admin user.
    /// </summary>
    public static bool CanAssignRole(string actorRole, string currentTargetRole, string newRole)
    {
        if (newRole != Admin || actorRole == Admin)
            return true;

        return currentTargetRole == Admin;
    }
}
