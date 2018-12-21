using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using Model;

	// Regression tests converted from tests for previous version of Merge API to new API.
	public partial class MergeTests
	{
		// ASE: just fails
		[Test]
		public void Merge([MergeDataContextSource(ProviderName.Sybase, ProviderName.SybaseManaged)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.GetTable<LinqDataTypes2>()
					.Merge()
					.Using(db.Types2)
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();
			}
		}

		// ASE: just fails
		[Test]
		public void MergeWithEmptySource([MergeDataContextSource(ProviderName.Sybase)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.GetTable<Person>()
					.Merge()
					.Using(new Person[] { })
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void MergeWithDelete([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<LinqDataTypes2>()
					.Merge()
					.Using(db.Types2)
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.DeleteWhenNotMatchedBySource()
					.Merge();
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void MergeWithDeletePredicate1([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<LinqDataTypes2>()
					.Merge()
					.Using(db.Types2.Where(t => t.ID > 5))
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.DeleteWhenNotMatchedBySourceAnd(t => t.ID > 5)
					.Merge();
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void MergeWithDeletePredicate3([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.Insert(new Person()
				{
					FirstName = "Не ASCII",
					Gender = Gender.Unknown,
					LastName = "Last Name",
					MiddleName = "Mid"
				});

				var person = db.Person.First();

				db.Insert(new Patient()
				{
					PersonID = person.ID,
					Diagnosis = "Negative"
				});

				var patient = db.Patient.Where(_ => _.PersonID == person.ID).First();
				db.GetTable<Person>()
					.Merge()
					.Using(db.Person.Where(t => t.Patient == patient))
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.DeleteWhenNotMatchedBySourceAnd(t => t.Patient == patient)
					.Merge();
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void MergeWithDeletePredicate4([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.Insert(new Person()
				{
					FirstName = "Не ASCII",
					Gender = Gender.Unknown,
					LastName = "Last Name",
					MiddleName = "Mid"
				});

				var person = db.Person.First();

				db.InsertOrReplace(new Patient()
				{
					PersonID = person.ID,
					Diagnosis = "Negative"
				});

				var patient = person.ID;
				var merge = db.GetTable<Person>()
					.Merge()
					.Using(db.Person.Where(t => t.Patient.PersonID == patient))
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.DeleteWhenNotMatchedBySourceAnd(t => t.Patient.PersonID == patient);
				merge.Merge();
				patient++;
				merge.Merge();
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void MergeWithDeletePredicate5([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<Child>()
					.Merge()
					.Using(db.Child.Where(t => t.Parent.ParentID == 2 && t.GrandChildren.Any(g => g.Child.ChildID == 22)))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.DeleteWhenNotMatchedBySourceAnd(t => t.Parent.ParentID == 2 && t.GrandChildren.Any(g => g.Child.ChildID == 22))
					.Merge();
			}
		}

		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		[Table("AllTypes")]
		class AllType
		{
			[PrimaryKey, Identity]
			public int ID;
			[Column(DataType = DataType.Char, Length = 1)]
			[Column("CHARDATATYPE", DataType = DataType.Char, Length = 1, Configuration = ProviderName.DB2)]
			public char charDataType;
			[Column(DataType = DataType.NChar, Length = 20)]
			public string ncharDataType;
			[Column(DataType = DataType.NVarChar, Length = 20)]
			public string nvarcharDataType;
		}

		// DB2: ncharDataType field missing in AllTypes
		// Informix: install the latest server
		[Test]
		public void MergeChar1([MergeDataContextSource(
			ProviderName.DB2, ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var id = ConvertTo<int>.From(db.GetTable<AllType>().InsertWithIdentity(() => new AllType
				{
					charDataType  = '\x0',
					ncharDataType = "\x0"
				}));

				db.GetTable<AllType>()
					.Merge()
					.Using(db.GetTable<AllType>().Where(t => t.ID == id))
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();
			}
		}

		// ASE: alltypes table must be fixed
		// DB2: ncharDataType field missing in AllTypes
		// Informix: install the latest server
		[Test]
		public void MergeChar2([MergeDataContextSource(
			ProviderName.DB2, ProviderName.Sybase, ProviderName.Informix)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<AllType>()
					.Merge()
					.Using(new[]
					{
						new AllType
						{
							ID            = 10,
							charDataType  = '\x0',
							ncharDataType = "\x0"
						}
					})
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();
			}
		}

		// extra test to check MergeChar* fixes (but we really need to implement excessive types tests for all providers)
		// ASE: AllTypes table must be fixed
		// DB2: ncharDataType and nvarcharDataType fields missing in AllTypes
		// Informix, SAP: looks like \0 terminates string
		[Test]
		public void MergeString([MergeDataContextSource(
			ProviderName.DB2, ProviderName.Sybase, ProviderName.Informix, ProviderName.SapHana)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var lastId = db.GetTable<AllType>().Select(_ => _.ID).Max();

				var rows = db.GetTable<AllType>()
					.Merge()
					.Using(new[]
					{
						new AllType()
						{
							ID = lastId + 1,
							charDataType = '\x0',
							ncharDataType = "\x0",
							nvarcharDataType = "test\x0it"
						}
					})
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				AssertRowCount(1, rows, context);

				var row = db.GetTable<AllType>().OrderByDescending(_ => _.ID).Take(1).Single();

				Assert.AreEqual('\0', row.charDataType);
				Assert.AreEqual("\0", row.ncharDataType);
				Assert.AreEqual("test\0it", row.nvarcharDataType);
			}
		}
	}
}
