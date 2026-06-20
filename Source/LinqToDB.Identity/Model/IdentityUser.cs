using System;
using System.Collections.Generic;
using ASP = Microsoft.AspNetCore.Identity;
using LinqToDB.Mapping;

namespace LinqToDB.Identity.Model
{
	/// <summary>
	/// The default implementation of <see cref="IdentityUser{TKey}" /> with navigation properties which uses a string as a primary key.
	/// </summary>
	public class IdentityUser : IdentityUser<string>
	{
		/// <summary>
		/// Initializes a new instance of <see cref="IdentityUser" />.
		/// </summary>
		/// <remarks>
		/// The Id property is initialized to from a new GUID string value.
		/// </remarks>
		public IdentityUser()
		{
			Id = Guid.NewGuid().ToString();
		}

		/// <summary>
		/// Initializes a new instance of <see cref="IdentityUser" />.
		/// </summary>
		/// <param name="userName">The user name.</param>
		/// <remarks>
		/// The Id property is initialized to from a new GUID string value.
		/// </remarks>
		public IdentityUser(string userName) : base(userName)
		{
			Id = Guid.NewGuid().ToString();
		}
	}

	/// <summary>
	/// Represents a user in the identity system with navigation properties.
	/// </summary>
	/// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
	public class IdentityUser<TKey> :
		IdentityUser<TKey, ASP.IdentityUserClaim<TKey>, ASP.IdentityUserRole<TKey>, ASP.IdentityUserLogin<TKey>>
		where TKey : IEquatable<TKey>
	{
		/// <summary>
		/// Initializes a new instance of <see cref="IdentityUser" />.
		/// </summary>
		/// <remarks>
		/// The Id property is initialized to from a new GUID string value.
		/// </remarks>
		public IdentityUser()
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="IdentityUser" />.
		/// </summary>
		/// <param name="userName">The user name.</param>
		/// <remarks>
		/// The Id property is initialized to from a new GUID string value.
		/// </remarks>
		public IdentityUser(string userName) : base(userName)
		{
		}
	}

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
		[Association(ThisKey = nameof(Id), OtherKey = nameof(ASP.IdentityUserRole<>.UserId))]
		public virtual ICollection<TUserRole>? Roles { get; set; }

		/// <summary>
		/// Navigation property for the claims this user possesses.
		/// </summary>
		[Association(ThisKey = nameof(Id), OtherKey = nameof(ASP.IdentityUserClaim<>.UserId))]
		public virtual ICollection<TUserClaim>? Claims { get; set; }

		/// <summary>
		/// Navigation property for this users login accounts.
		/// </summary>
		[Association(ThisKey = nameof(Id), OtherKey = nameof(ASP.IdentityUserLogin<>.UserId))]
		public virtual ICollection<TUserLogin>? Logins { get; set; }
	}
}
