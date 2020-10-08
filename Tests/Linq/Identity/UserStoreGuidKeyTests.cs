// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Identity
{
	public class GuidUser : LinqToDB.Identity.IdentityUser<Guid>
	{
		public GuidUser()
		{
			Id = Guid.NewGuid();
			UserName = Id.ToString();
		}
	}

	public class GuidRole : LinqToDB.Identity.IdentityRole<Guid>
	{
		public GuidRole()
		{
			Id = Guid.NewGuid();
			Name = Id.ToString();
		}
	}

	public class UserStoreGuidTests : SqlStoreTestBase<GuidUser, GuidRole, Guid>
	{
		protected override void AddUserStore(IServiceCollection services, TestConnectionFactory? context = null)
		{
			services.AddSingleton<IUserStore<GuidUser>>(new ApplicationUserStore(context ?? CreateTestContext()));
		}

		protected override void AddRoleStore(IServiceCollection services, TestConnectionFactory? context = null)
		{
			services.AddSingleton<IRoleStore<GuidRole>>(new ApplicationRoleStore(context ?? CreateTestContext()));
		}

		public class ApplicationUserStore : UserStore<Guid, GuidUser, GuidRole>
		{
			public ApplicationUserStore(IConnectionFactory factory) : base(factory)
			{
			}
		}

		public class ApplicationRoleStore : RoleStore<Guid, GuidRole>
		{
			public ApplicationRoleStore(IConnectionFactory factory) : base(factory)
			{
			}
		}
	}
}
