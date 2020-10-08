using System;

namespace Tests.Identity
{
	public class IntUser : LinqToDB.Identity.IdentityUser<int>
	{
		private static volatile int _id;

		public IntUser()
		{
			Id = ++_id;
			UserName = Guid.NewGuid().ToString();
		}
	}

	public class IntRole : LinqToDB.Identity.IdentityRole<int>
	{
		private static volatile int _id;

		public IntRole()
		{
			Id = ++_id;
			Name = Guid.NewGuid().ToString();
		}
	}

	public class UserStoreIntTest : SqlStoreTestBase<IntUser, IntRole, int>
	{
	}
}
