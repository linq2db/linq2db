using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Mapping;

using Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity
{
	/// <summary>
	/// linq2db association navigations for the standard ASP.NET Core Identity entity types, exposed as extension
	/// methods so they attach to <see cref="IdentityUser{TKey}"/> / <see cref="IdentityRole{TKey}"/> directly — no
	/// derived "model" type is required. They are association markers: usable only inside a LINQ query expression
	/// (e.g. <c>from r in user.Roles() select r.Name</c> or <c>users.SelectMany(u =&gt; u.Claims())</c>), where
	/// linq2db expands them into the corresponding join. Invoking outside a query throws.
	/// </summary>
	/// <remarks>
	/// These are methods rather than properties because C# forbids extension-property access inside expression trees
	/// (CS9296), and linq2db queries are expression trees.
	/// </remarks>
	public static class IdentityNavigations
	{
		const string AssociationOnly = "Identity navigation is a linq2db association and can only be used inside a LINQ query expression.";

		/// <summary>
		/// Roles this user belongs to, through <c>AspNetUserRoles</c>. Returns the stock <see cref="IdentityRole{TKey}"/>
		/// over <c>AspNetRoles</c>; with a custom role type, join on <see cref="IdentityRole{TKey}.Id"/> for its extra members.
		/// </summary>
		[ExpressionMethod(nameof(RolesExpression))]
		public static IQueryable<IdentityRole<TKey>> Roles<TKey>(this IdentityUser<TKey> user)
			where TKey : IEquatable<TKey> => throw new InvalidOperationException(AssociationOnly);

		static Expression<Func<IdentityUser<TKey>, IQueryable<IdentityRole<TKey>>>> RolesExpression<TKey>()
			where TKey : IEquatable<TKey>
			=> user => user.UserRoles().Select(ur => ur.Role());

		/// <summary>Claims this user possesses (join <c>AspNetUsers.Id</c> → <c>AspNetUserClaims.UserId</c>).</summary>
		[Association(ThisKey = nameof(IdentityUser<>.Id), OtherKey = nameof(IdentityUserClaim<>.UserId))]
		public static IEnumerable<IdentityUserClaim<TKey>> Claims<TKey>(this IdentityUser<TKey> user)
			where TKey : IEquatable<TKey> => throw new InvalidOperationException(AssociationOnly);

		/// <summary>External login accounts for this user (join <c>AspNetUsers.Id</c> → <c>AspNetUserLogins.UserId</c>).</summary>
		[Association(ThisKey = nameof(IdentityUser<>.Id), OtherKey = nameof(IdentityUserLogin<>.UserId))]
		public static IEnumerable<IdentityUserLogin<TKey>> Logins<TKey>(this IdentityUser<TKey> user)
			where TKey : IEquatable<TKey> => throw new InvalidOperationException(AssociationOnly);

		/// <summary>
		/// Users in this role, through <c>AspNetUserRoles</c>. Returns the stock <see cref="IdentityUser{TKey}"/> over
		/// <c>AspNetUsers</c>; with a custom user type, join on <see cref="IdentityUser{TKey}.Id"/> for its extra members.
		/// </summary>
		[ExpressionMethod(nameof(UsersExpression))]
		public static IQueryable<IdentityUser<TKey>> Users<TKey>(this IdentityRole<TKey> role)
			where TKey : IEquatable<TKey> => throw new InvalidOperationException(AssociationOnly);

		static Expression<Func<IdentityRole<TKey>, IQueryable<IdentityUser<TKey>>>> UsersExpression<TKey>()
			where TKey : IEquatable<TKey>
			=> role => role.UserRoles().Select(ur => ur.User());

		/// <summary>Claims granted to this role (join <c>AspNetRoles.Id</c> → <c>AspNetRoleClaims.RoleId</c>).</summary>
		[Association(ThisKey = nameof(IdentityRole<>.Id), OtherKey = nameof(IdentityRoleClaim<>.RoleId))]
		public static IEnumerable<IdentityRoleClaim<TKey>> Claims<TKey>(this IdentityRole<TKey> role)
			where TKey : IEquatable<TKey> => throw new InvalidOperationException(AssociationOnly);

		// the AspNetUserRoles hops behind Roles() / Users()
		[Association(ThisKey = nameof(IdentityUser<>.Id), OtherKey = nameof(IdentityUserRole<>.UserId))]
		static IQueryable<IdentityUserRole<TKey>> UserRoles<TKey>(this IdentityUser<TKey> user)
			where TKey : IEquatable<TKey> => throw new InvalidOperationException(AssociationOnly);

		[Association(ThisKey = nameof(IdentityRole<>.Id), OtherKey = nameof(IdentityUserRole<>.RoleId))]
		static IQueryable<IdentityUserRole<TKey>> UserRoles<TKey>(this IdentityRole<TKey> role)
			where TKey : IEquatable<TKey> => throw new InvalidOperationException(AssociationOnly);

		[Association(ThisKey = nameof(IdentityUserRole<>.RoleId), OtherKey = nameof(IdentityRole<>.Id), CanBeNull = false)]
		static IdentityRole<TKey> Role<TKey>(this IdentityUserRole<TKey> userRole)
			where TKey : IEquatable<TKey> => throw new InvalidOperationException(AssociationOnly);

		[Association(ThisKey = nameof(IdentityUserRole<>.UserId), OtherKey = nameof(IdentityUser<>.Id), CanBeNull = false)]
		static IdentityUser<TKey> User<TKey>(this IdentityUserRole<TKey> userRole)
			where TKey : IEquatable<TKey> => throw new InvalidOperationException(AssociationOnly);
	}
}
