namespace LinqToDB.Identity
{
	internal static class Resources
	{
		public const string ValueCannotBeNullOrEmpty = "Value cannot be null or empty.";
		public const string NotIdentityUser          = "AddLinqToDBStores can only be called with a user that derives from IdentityUser<TKey>.";
		public const string NotIdentityRole          = "AddLinqToDBStores can only be called with a role that derives from IdentityRole<TKey>.";

		public static string RoleNotFound(string roleName) => $"Role {roleName} does not exist.";
	}
}
