using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using Model;

	[TestFixture]
	[Order(10000)]
	[Obsolete("Tests for obsolete API. Will be removed with API under question.")]
	public class OldMergeTests : TestBase
	{
		[Test]
		public void Merge(
			[DataSources(
				false,
				TestProvName.AllAccess,
				TestProvName.AllInformix,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				ProviderName.SqlCe,
				TestProvName.AllClickHouse,
				TestProvName.AllSqlServer2005,
				TestProvName.AllSybase,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.Merge(db.Types2);
			}
		}

		[Test]
		public void MergeWithEmptySource(
			[DataSources(
				false,
				TestProvName.AllAccess,
				TestProvName.AllInformix,
				TestProvName.AllSQLite,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer2005)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.Merge(Array.Empty<Person>());
			}
		}

		[Test]
		public void MergeWithDelete(
			[DataSources(
				false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer2005)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.Merge(true, db.Types2);
			}
		}

		[Test]
		public void MergeWithDeletePredicate1(
			[DataSources(
				false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer2005)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.Merge(t => t.ID > 5, db.Types2.Where(t => t.ID > 5));
			}
		}

		[Test]
		public void MergeWithDeletePredicate2(
			[DataSources(
				false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer2005)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.Merge(db.Types2, t => t.ID > 5);
			}
		}

		[Test]
		public async Task MergeWithDeletePredicate2Async(
			[DataSources(
				false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer2005)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				await db.MergeAsync(db.Types2, t => t.ID > 5);
			}
		}

		[Test]
		public void MergeWithDeletePredicate3(
			[DataSources(
				false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer2005)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				var patient = db.Patient.First();
				db.Merge(db.Person, t => t.Patient == patient);
			}
		}

		[Test]
		public void MergeWithDeletePredicate4(
			[DataSources(
				false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer2005)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				var patient = db.Patient.First().PersonID;
				db.Merge(db.Person, t => t.Patient!.PersonID == patient);
				patient++;
				db.Merge(db.Person, t => t.Patient!.PersonID == patient);
			}
		}

		[Test]
		public void MergeWithDeletePredicate5(
			[DataSources(
				false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer2005)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.Merge(db.Child, t => t.Parent!.ParentID == 2 && t.GrandChildren.Any(g => g.Child!.ChildID == 22));
			}
		}

		[Table("AllTypes")]
		sealed class AllType
		{
			[PrimaryKey, Identity] public int ID;
			[Column(DataType = DataType.Char,  Length = 1)]  public char    charDataType;
			[Column(DataType = DataType.NChar, Length = 20)] public string? ncharDataType;
		}

		[Test]
		public void MergeChar1(
			[DataSources(
				false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer2005)]
			string context)
		{
			ResetAllTypesIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				var id = ConvertTo<int>.From(db.GetTable<AllType>().InsertWithIdentity(() => new AllType
				{
					charDataType  = '\x0',
					ncharDataType = "\x0"
				}));

				db.Merge(db.GetTable<AllType>().Where(t => t.ID == id));
			}
		}

		[Test]
		public void MergeChar2(
			[DataSources(
				false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.DB2,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer2005)]
			string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				db.Merge(new[]
				{
					new AllType
					{
						ID            = 10,
						charDataType  = '\x0',
						ncharDataType = "\x0"
					}
				});
			}
		}
	}
}
