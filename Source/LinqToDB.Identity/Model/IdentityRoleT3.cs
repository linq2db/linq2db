using System;
using System.Collections.Generic;
using LinqToDB.Mapping;
using ASP = Microsoft.AspNetCore.Identity;

namespace LinqToDB.Identity.Model
{
	/// <summary>
	/// Implementation of <see cref="ASP.IdentityRole{TKey}" /> with navigation properties.
	/// </summary>
	public class IdentityRole<TKey, TUserRole, TRoleClaim> : ASP.IdentityRole<TKey>
		where TKey       : IEquatable<TKey>
		where TUserRole  : ASP.IdentityUserRole<TKey>
		where TRoleClaim : ASP.IdentityRoleClaim<TKey>
	{
		/// <summary>
		/// Initializes a new instance of <see cref="IdentityRole{TUserRole, TRoleClaim}"/>.
		/// </summary>
		public IdentityRole()
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="IdentityRole{TUserRole, TRoleClaim}"/>.
		/// </summary>
		/// <param name="roleName">The role name.</param>
		public IdentityRole(string roleName) : base(roleName)
		{
		}

		/// <summary>
		/// Navigation property for users with this role.
		/// </summary>
		[Association(ThisKey = nameof(Id), OtherKey = nameof(ASP.IdentityUserRole<TKey>.RoleId))]
		public virtual ICollection<TUserRole>? Users { get; set; }

		/// <summary>
		/// Navigation property for this role claims.
		/// </summary>
		[Association(ThisKey = nameof(Id), OtherKey = nameof(ASP.IdentityRoleClaim<TKey>.RoleId))]
		public virtual ICollection<TRoleClaim>? Claims { get; set; }

		/// <inheritdoc cref="Id" />
		[Column(CanBeNull = false, IsPrimaryKey = true)]
		public override TKey Id { get; set; } = default!;
	}
}
