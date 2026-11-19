using System.Collections.Generic;

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
    /// Roles that can be assigned to a user of the given organisation:
    /// - User: always;
    /// - User Admin: if the organisation is marked AllowsUserAdmin
    ///   (USER_ADMIN_ORGANIZATION, e.g. Save the Children) OR AllowsOrganizationAdmin
    ///   (UNICEF can also hold User Admin, e.g. to take over user management);
    /// - Organisation Admin: only if marked AllowsOrganizationAdmin
    ///   (ORGANIZATION_ADMIN_ORGANIZATION, e.g. UNICEF);
    /// - Admin: unchanged rule — only an Admin can grant it, in any organisation;
    ///   a user who already is Admin keeps it when others edit them.
    /// An existing User Admin / Organisation Admin in an organisation without the
    /// mark is NOT kept: the role must be corrected when the user is next edited.
    /// </summary>
    public static List<string> AssignableRoles(
        string actorRole,
        bool allowsUserAdmin,
        bool allowsOrganizationAdmin,
        string currentTargetRole = null
    )
    {
        var roles = new List<string>();
        if (actorRole == Admin || currentTargetRole == Admin)
            roles.Add(Admin);
        if (allowsUserAdmin || allowsOrganizationAdmin)
            roles.Add(UserAdmin);
        if (allowsOrganizationAdmin)
            roles.Add(OrganizationAdmin);
        roles.Add(User);
        return roles;
    }

    public static string Label(string role) => role switch
    {
        Admin => "Admin",
        UserAdmin => "User Admin",
        OrganizationAdmin => "Organisation Admin",
        User => "User",
        _ => role,
    };
}
