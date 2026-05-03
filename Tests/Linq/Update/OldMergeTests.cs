using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.xUpdate
{
	[TestFixture]
	[Order(10000)]
	[Obsolete("Tests for obsolete API. Will be removed with API under question.")]
	public class OldMergeTests : TestBase
	{
		[Test]
		public void Merge(
			[MergeDataContextSource(
				false,
				TestProvName.AllInformix,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSybase,
				TestProvName.AllSapHana)]
			string context)
		{
			using var db = GetDataConnection(context);
			db.Merge(db.Types2);
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
			using var db = GetDataConnection(context);
			db.Merge(Array.Empty<Person>());
		}

		[Test]
		public void MergeWithDelete(
			[MergeDataContextSource(
				false,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using var db = GetDataConnection(context);
			db.Merge(true, db.Types2);
		}

		[Test]
		public void MergeWithDeletePredicate1(
			[MergeDataContextSource(
				false,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using var db = GetDataConnection(context);
			db.Merge(t => t.ID > 5, db.Types2.Where(t => t.ID > 5));
		}

		[Test]
		public void MergeWithDeletePredicate2(
			[MergeDataContextSource(
				false,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using var db = GetDataConnection(context);
			db.Merge(db.Types2, t => t.ID > 5);
		}

		[Test]
		public async Task MergeWithDeletePredicate2Async(
			[MergeDataContextSource(
				false,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using var db = GetDataConnection(context);
			await db.MergeAsync(db.Types2, t => t.ID > 5);
		}

		[Test]
		public void MergeWithDeletePredicate3(
			[MergeDataContextSource(
				false,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using var db = GetDataConnection(context);
			var patient = db.Patient.First();
			db.Merge(db.Person, t => t.Patient == patient);
		}

		[Test]
		public void MergeWithDeletePredicate4(
			[MergeDataContextSource(
				false,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using var db = GetDataConnection(context);
			var patient = db.Patient.First().PersonID;
			db.Merge(db.Person, t => t.Patient!.PersonID == patient);
			patient++;
			db.Merge(db.Person, t => t.Patient!.PersonID == patient);
		}

		[Test]
		public void MergeWithDeletePredicate5(
			[MergeDataContextSource(
				false,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using var db = GetDataConnection(context);
			db.Merge(db.Child, t => t.Parent!.ParentID == 2 && t.GrandChildren.Any(g => g.Child!.ChildID == 22));
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
			[MergeDataContextSource(
				false,
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllSapHana)]
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
			[MergeDataContextSource(
				false,
				ProviderName.DB2,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracle,
				TestProvName.AllSybase,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
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
