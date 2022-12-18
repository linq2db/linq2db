using System;
using System.Collections.Generic;
using LinqToDB.Mapping;
using ASP = Microsoft.AspNetCore.Identity;

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

		/// <summary>
		/// Gets or sets the primary key for this user.
		/// </summary>
		[Column(CanBeNull = false, IsPrimaryKey = true, Length = 255)]
		public override string Id { get; set; } = default!;
	}
}
