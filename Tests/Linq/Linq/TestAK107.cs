using System;

using LinqToDB;
using LinqToDB.Data.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class TestAK107 : TestBase
	{
		[TableName(Name = "t_test_user")]
		public sealed class User
		{
			[PrimaryKey, Identity]
			[SequenceName("sq_test_user")]
			[MapField("user_id")]
			public long Id { get; set; }

			[NotNull, NonUpdatable(OnInsert = false)]
			[MapField("name")]
			public string Name { get; set; }
		}

		[TableName(Name = "t_test_user_contract")]
		public sealed class Contract
		{
			[PrimaryKey, Identity]
			[SequenceName("sq_test_user_contract")]
			[MapField("user_contract_id")]
			public long Id { get; set; }

			[NotNull, NonUpdatable(OnInsert = false)]
			[MapField("user_id")]
			public long UserId { get; set; }

			[Association(ThisKey = "UserId", OtherKey = "Id", CanBeNull = false)]
			public User User { get; set; }

			[NotNull, NonUpdatable(OnInsert = false)]
			[MapField("contract_no")]
			public long ContractNo { get; set; }

			[NotNull]
			[MapField("name")]
			public string Name { get; set; }
		}

		[Test]
		public void UserInsert()
		{
			using (var db = new TestDbManager("Oracle"))
			{
				db.BeginTransaction();
				db.Insert(new User { Name = "user" });
			}
		}

		[Test]
		public void UserInsertWithIdentity()
		{
			using (var db = new TestDbManager("Oracle"))
			{
				db.BeginTransaction();
				db.InsertWithIdentity(new User { Name = "user" });
			}
		}

		[Test]
		public void UserLinqInsert()
		{
			using (var db = new TestDbManager("Oracle"))
			{
				db.BeginTransaction();
				db.GetTable<User>().Insert(() => new User { Name = "user" });
			}
		}

		[Test]
		public void UserLinqInsertWithIdentity()
		{
			using (var db = new TestDbManager("Oracle"))
			{
				db.BeginTransaction();
				db.GetTable<User>().InsertWithIdentity(() => new User { Name = "user" });
			}
		}

		[Test]
		public void ContractInsert()
		{
			using (var db = new TestDbManager("Oracle"))
			{
				db.BeginTransaction();

				var user = new User { Name = "user" };
				user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

				db.Insert(new Contract { UserId = user.Id, ContractNo = 1, Name = "contract1" });
			}
		}

		[Test]
		public void ContractInsertWithIdentity()
		{
			using (var db = new TestDbManager("Oracle"))
			{
				db.BeginTransaction();

				var user = new User { Name = "user" };
				user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

				db.InsertWithIdentity(new Contract { UserId = user.Id, ContractNo = 1, Name = "contract" });
			}
		}

		[SqlExpression("sq_test_user_contract.nextval")]
		static long ContractSequence { get; set;  }

		[Test]
		public void ContractLinqInsert()
		{
			using (var db = new TestDbManager("Oracle"))
			{
				db.BeginTransaction();

				var user = new User { Name = "user" };
				user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

				db.GetTable<Contract>().Insert(() => new Contract
				{
					Id         = ContractSequence,
					UserId     = user.Id,
					ContractNo = 1,
					Name       = "contract"
				});
			}
		}

		[Test]
		public void ContractLinqInsertWithIdentity()
		{
			using (var db = new TestDbManager("Oracle"))
			{
				db.BeginTransaction();

				var user = new User { Name = "user" };
				user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

				db.GetTable<Contract>().InsertWithIdentity(() => new Contract { UserId = user.Id, ContractNo = 1, Name = "contract" });
			}
		}

		[Test]
		public void ContractLinqManyInsert()
		{
			using (var db = new TestDbManager("Oracle"))
			{
				db.BeginTransaction();

				var user = new User { Name = "user" };
				user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

				db.GetTable<User>().Insert(db.GetTable<Contract>(), x => new Contract { UserId = x.Id, ContractNo = 1, Name = "contract" });
			}
		}

		//[Test]
		public void ContractLinqManyInsertWithIdentity()
		{
			using (var db = new TestDbManager("Oracle"))
			{
				db.BeginTransaction();

				var user = new User { Name = "user" };
				user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

				db.GetTable<User>().InsertWithIdentity(db.GetTable<Contract>(), x => new Contract
				{
					UserId = x.Id, ContractNo = 1, Name = "contract"
				});
			}
		}
	}
}
