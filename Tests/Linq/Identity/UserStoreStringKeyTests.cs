using System;
using LinqToDB;
using LinqToDB.Mapping;

namespace Tests.Identity
{
	public class StringUser : LinqToDB.Identity.IdentityUser
	{
		public StringUser()
		{
			Id = Guid.NewGuid().ToString();
			UserName = Id;
		}

		[Column(DataType = DataType.VarChar, Length = 36, IsPrimaryKey = true, SkipOnUpdate = true, CanBeNull = false)]
		public override string Id { get; set; }
	}

	public class StringRole : LinqToDB.Identity.IdentityRole<string>
	{
		public StringRole()
		{
			Id = Guid.NewGuid().ToString();
			Name = Id;
		}

		[Column(DataType = DataType.VarChar, Length = 36, IsPrimaryKey = true, SkipOnUpdate = true, CanBeNull = false)]
		public override string Id { get; set; }
	}

	public class UserStoreStringKeyTests : SqlStoreTestBase<StringUser, StringRole, string>
	{
	}
}
