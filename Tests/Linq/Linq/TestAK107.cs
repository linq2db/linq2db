using System;

using LinqToDB;
using LinqToDB.Linq;
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
		public void UserInsert([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			using (var db = new TestDbManager(context))
			{
				db.BeginTransaction();
				db.Insert(new User { Name = "user" });
			}
		}

		[Test]
		public void UserInsertWithIdentity([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			using (var db = new TestDbManager(context))
			{
				db.BeginTransaction();
				db.InsertWithIdentity(new User { Name = "user" });
			}
		}

		[Test]
		public void UserLinqInsert([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			using (var db = new TestDbManager(context))
			{
				db.BeginTransaction();
				db.GetTable<User>().Insert(() => new User { Name = "user" });
			}
		}

		[Test]
		public void UserLinqInsertWithIdentity([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			using (var db = new TestDbManager(context))
			{
				db.BeginTransaction();
				db.GetTable<User>().InsertWithIdentity(() => new User { Name = "user" });
			}
		}

		[Test]
		public void ContractInsert([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			using (var db = new TestDbManager(context))
			{
				db.BeginTransaction();

				var user = new User { Name = "user" };
				user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

				db.Insert(new Contract { UserId = user.Id, ContractNo = 1, Name = "contract1" });
			}
		}

		[Test]
		public void ContractInsertWithIdentity([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			using (var db = new TestDbManager(context))
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
		public void ContractLinqInsert([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			using (var db = new TestDbManager(context))
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
		public void ContractLinqInsertWithIdentity([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			using (var db = new TestDbManager(context))
			{
				db.BeginTransaction();

				var user = new User { Name = "user" };
				user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

				db.GetTable<Contract>().InsertWithIdentity(() => new Contract { UserId = user.Id, ContractNo = 1, Name = "contract" });
			}
		}

		[Test]
		public void ContractLinqManyInsert([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			using (var db = new TestDbManager(context))
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
