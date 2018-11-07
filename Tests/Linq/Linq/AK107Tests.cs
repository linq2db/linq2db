using System;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class AK107Tests : TestBase
	{
		[Table(Name = "t_test_user")]
		public sealed class User
		{
			[Column("user_id"), PrimaryKey, Identity]
			[SequenceName("sq_test_user")]
			public long Id { get; set; }

			[Column("name", SkipOnUpdate=true), NotNull]
			public string Name { get; set; }
		}

		[Table(Name = "t_test_user_contract")]
		public sealed class Contract
		{
			[Column("user_contract_id", IsIdentity=true), PrimaryKey]
			[SequenceName("sq_test_user_contract")]
			public long Id { get; set; }

			[Column("user_id", SkipOnUpdate = true, CanBeNull = false)]
			public long UserId { get; set; }

			[Association(ThisKey = "UserId", OtherKey = "Id", CanBeNull = false)]
			public User User { get; set; }

			[Column("contract_no", SkipOnUpdate = true, CanBeNull = false)]
			public long ContractNo { get; set; }

			[Column("name"), NotNull]
			public string Name { get; set; }
		}

		[Test]
		public void UserInsert([IncludeDataSources(
			ProviderName.OracleNative, ProviderName.OracleManaged)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();
				db.Insert(new User { Name = "user" });
			}
		}

		[Test]
		public void UserInsertWithIdentity([IncludeDataSources(
			ProviderName.OracleNative, ProviderName.OracleManaged)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();
				db.InsertWithIdentity(new User { Name = "user" });
			}
		}

		[Test]
		public void UserLinqInsert([IncludeDataSources(
			ProviderName.OracleNative, ProviderName.OracleManaged)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();
				db.GetTable<User>().Insert(() => new User { Name = "user" });
			}
		}

		[Test]
		public void UserLinqInsertWithIdentity([IncludeDataSources(
			ProviderName.OracleNative, ProviderName.OracleManaged)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();
				db.GetTable<User>().InsertWithIdentity(() => new User { Name = "user" });
			}
		}

		[Test]
		public void ContractInsert([IncludeDataSources(
			ProviderName.OracleNative, ProviderName.OracleManaged)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				var user = new User { Name = "user" };
				user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

				db.Insert(new Contract { UserId = user.Id, ContractNo = 1, Name = "contract1" });
			}
		}

		[Test]
		public void ContractInsertWithIdentity([IncludeDataSources(
			ProviderName.OracleNative, ProviderName.OracleManaged)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				var user = new User { Name = "user" };
				user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

				db.InsertWithIdentity(new Contract { UserId = user.Id, ContractNo = 1, Name = "contract" });
			}
		}

		[Sql.Expression("sq_test_user_contract.nextval")]
		static long ContractSequence { get; set;  }

		[Test]
		public void ContractLinqInsert([IncludeDataSources(
			ProviderName.OracleNative, ProviderName.OracleManaged)]
			string context)
		{
			using (var db = GetDataContext(context))
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
		public void ContractLinqInsertWithIdentity([IncludeDataSources(
			ProviderName.OracleNative, ProviderName.OracleManaged)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				var user = new User { Name = "user" };
				user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

				db.GetTable<Contract>().InsertWithIdentity(() => new Contract { UserId = user.Id, ContractNo = 1, Name = "contract" });
			}
		}

		[Test]
		public void ContractLinqManyInsert([IncludeDataSources(
			ProviderName.OracleNative, ProviderName.OracleManaged)]
			string context)
		{
			using (var db = GetDataContext(context))
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
			using (var db = GetDataContext("Oracle"))
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
