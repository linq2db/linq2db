using System;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class AK107Tests : TestBase
	{
		[Table(Name = "t_test_user")]
		public sealed class User
		{
			[Column("user_id"), PrimaryKey, Identity]
			public long Id { get; set; }

			[Column("name", SkipOnUpdate=true), NotNull]
			public string Name { get; set; } = null!;
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
			public User User { get; set; } = null!;

			[Column("contract_no", SkipOnUpdate = true, CanBeNull = false)]
			public long ContractNo { get; set; }

			[Column("name"), NotNull]
			public string Name { get; set; } = null!;
		}

		private MappingSchema CreateUser(string context)
		{
			using var _ = new DisableBaseline("test setup");
			//var schema = context.IsAnyOf(TestProvName.AllOracle19) ? "sequence_schema" : "c##sequence_schema";
			var schema = "c##sequence_schema";

			using var db = GetDataContext(context);
			try
			{
				db.Execute($"DROP USER \"{schema}\" CASCADE");
			}
			catch { }

			db.Execute($"CREATE USER \"{schema}\" IDENTIFIED BY \"secret_password\"");
			db.Execute($"GRANT CREATE SEQUENCE TO \"{schema}\"");
			db.Execute($"create sequence \"{schema}\".\"sq_test_user\"");

			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<User>()
				.Property(e => e.Id)
				.UseSequence("sq_test_user", schema: schema)
				.Build();
			return ms;
		}

		[Test]
		public void UserInsert([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context, CreateUser(context));
			db.BeginTransaction();
			db.Insert(new User { Name = "user" });
		}

		[Test]
		public void UserInsertWithIdentity([IncludeDataSources(TestProvName.AllOracle)]
			string context)
		{
			using var db = GetDataContext(context, CreateUser(context));
			db.BeginTransaction();
			db.InsertWithIdentity(new User { Name = "user" });
		}

		[Test]
		public void UserLinqInsert([IncludeDataSources(TestProvName.AllOracle)]
			string context)
		{
			using var db = GetDataContext(context, CreateUser(context));
			db.BeginTransaction();
			db.GetTable<User>().Insert(() => new User { Name = "user" });
		}

		[Test]
		public void UserLinqInsertWithIdentity([IncludeDataSources(TestProvName.AllOracle)]
			string context)
		{
			using var db = GetDataContext(context, CreateUser(context));
			db.BeginTransaction();
			db.GetTable<User>().InsertWithIdentity(() => new User { Name = "user" });
		}

		[Test]
		public void ContractInsert([IncludeDataSources(TestProvName.AllOracle)]
			string context)
		{
			using var db = GetDataContext(context, CreateUser(context));
			db.BeginTransaction();

			var user = new User { Name = "user" };
			user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

			db.Insert(new Contract { UserId = user.Id, ContractNo = 1, Name = "contract1" });
		}

		[Test]
		public void ContractInsertWithIdentity([IncludeDataSources(TestProvName.AllOracle)]
			string context)
		{
			using var db = GetDataContext(context, CreateUser(context));
			db.BeginTransaction();

			var user = new User { Name = "user" };
			user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

			db.InsertWithIdentity(new Contract { UserId = user.Id, ContractNo = 1, Name = "contract" });
		}

		[Sql.Expression("sq_test_user_contract.nextval")]
		static long ContractSequence { get; set;  }

		[Test]
		public void ContractLinqInsert([IncludeDataSources(TestProvName.AllOracle)]
			string context)
		{
			using var db = GetDataContext(context, CreateUser(context));
			db.BeginTransaction();

			var user = new User { Name = "user" };
			user.Id  = Convert.ToInt64(db.InsertWithIdentity(user));

			db.GetTable<Contract>().Insert(() => new Contract
			{
				Id         = ContractSequence,
				UserId     = user.Id,
				ContractNo = 1,
				Name       = "contract"
			});
		}

		[Test]
		public void ContractLinqInsertWithIdentity([IncludeDataSources(TestProvName.AllOracle)]
			string context)
		{
			using var db = GetDataContext(context, CreateUser(context));
			db.BeginTransaction();

			var user = new User { Name = "user" };
			user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

			db.GetTable<Contract>().InsertWithIdentity(() => new Contract { UserId = user.Id, ContractNo = 1, Name = "contract" });
		}

		[Test]
		public void ContractLinqManyInsert([IncludeDataSources(TestProvName.AllOracle)]
			string context)
		{
			using var db = GetDataContext(context, CreateUser(context));
			db.BeginTransaction();

			var user = new User { Name = "user" };
			user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

			db.GetTable<User>().Insert(db.GetTable<Contract>(), x => new Contract { UserId = x.Id, ContractNo = 1, Name = "contract" });
		}

		//[Test]
		//public void ContractLinqManyInsertWithIdentity()
		//{
		//	using (var db = GetDataContext("Oracle"))
		//	{
		//		db.BeginTransaction();

		//		var user = new User { Name = "user" };
		//		user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

		//		db.GetTable<User>().InsertWithIdentity(db.GetTable<Contract>(), x => new Contract
		//		{
		//			UserId = x.Id, ContractNo = 1, Name = "contract"
		//		});
		//	}
		//}

		[Test]
		public void SequenceNameTest([IncludeDataSources(false, TestProvName.AllOracle)]
			string context)
		{
			//var schema = context.IsAnyOf(TestProvName.AllOracle19) ? "sequence_schema" : "c##sequence_schema";
			var schema = "c##sequence_schema";
			using var db = GetDataConnection(context, CreateUser(context));
			db.BeginTransaction();

			var user = new User { Name = "user" };
			user.Id = Convert.ToInt64(db.InsertWithIdentity(user));

			Assert.That(db.LastQuery?.Contains($"\"{schema}\".\"sq_test_user\".nextval"), Is.True);

			db.Insert(new Contract { UserId = user.Id, ContractNo = 1, Name = "contract1" });

			Assert.That(db.LastQuery?.Contains("\t\"sq_test_user_contract\".nextval"), Is.True);
		}
	}
}
