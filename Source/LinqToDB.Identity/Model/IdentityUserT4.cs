using System;
using System.Collections.Generic;
using LinqToDB.Mapping;
using ASP = Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity.Model
{
	/// <summary>
	/// Represents a user in the identity system with navigation properties.
	/// </summary>
	/// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
	/// <typeparam name="TUserClaim">The type representing a claim.</typeparam>
	/// <typeparam name="TUserRole">The type representing a user role.</typeparam>
	/// <typeparam name="TUserLogin">The type representing a user external login.</typeparam>
	public class IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin> : ASP.IdentityUser<TKey> where TKey : IEquatable<TKey>
	{
		/// <summary>
		/// Initializes a new instance of <see cref="IdentityUser{TKey}" />.
		/// </summary>
		public IdentityUser()
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="IdentityUser{TKey}" />.
		/// </summary>
		/// <param name="userName">The user name.</param>
		public IdentityUser(string userName)
		{
			UserName = userName;
		}

		/// <summary>
		/// Navigation property for the roles this user belongs to.
		/// </summary>
		[Association(ThisKey = nameof(Id), OtherKey = nameof(ASP.IdentityUserRole<TKey>.UserId))]
		public virtual ICollection<TUserRole>? Roles { get; set; }

		/// <summary>
		/// Navigation property for the claims this user possesses.
		/// </summary>
		[Association(ThisKey = nameof(Id), OtherKey = nameof(ASP.IdentityUserClaim<TKey>.UserId))]
		public virtual ICollection<TUserClaim>? Claims { get; set; }

		/// <summary>
		/// Navigation property for this users login accounts.
		/// </summary>
		[Association(ThisKey = nameof(Id), OtherKey = nameof(ASP.IdentityUserLogin<TKey>.UserId))]
		public virtual ICollection<TUserLogin>? Logins { get; set; }

		/// <summary>
		/// Gets or sets the primary key for this user.
		/// </summary>
		[Column(CanBeNull = false, IsPrimaryKey = true)]
		public override TKey Id { get; set; } = default!;
	}
}
