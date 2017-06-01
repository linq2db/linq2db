using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;
using Tests.Model;
using LinqToDB.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.DataProvider;

namespace Tests.Merge
{
	// Regression tests converted from tests for previous version of Merge API to new API.
	public partial class MergeTests
	{
		[MergeDataContextSource(ProviderName.Sybase)]
		public void Merge(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.GetTable<LinqDataTypes2>().FromSame(db.Types2)
					.Update().Insert()
					.Merge();
			}
		}

		[MergeDataContextSource(ProviderName.Sybase)]
		public void MergeWithEmptySource(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.GetTable<Person>().FromSame(new Person[] { })
					.Update().Insert()
					.Merge();
			}
		}

		[MergeBySourceDataContextSource]
		public void MergeWithDelete(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<LinqDataTypes2>().FromSame(db.Types2)
					.Update().Insert().DeleteBySource()
					.Merge();
			}
		}

		[MergeBySourceDataContextSource]
		public void MergeWithDeletePredicate1(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<LinqDataTypes2>().FromSame(db.Types2.Where(t => t.ID > 5))
					.Update().Insert().DeleteBySource(t => t.ID > 5)
					.Merge();
			}
		}

		[MergeBySourceDataContextSource]
		public void MergeWithDeletePredicate3(string context)
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
				db.GetTable<Person>().FromSame(db.Person.Where(t => t.Patient == patient))
					.Update().Insert().DeleteBySource(t => t.Patient == patient)
					.Merge();
			}
		}

		[MergeBySourceDataContextSource]
		public void MergeWithDeletePredicate4(string context)
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
					.FromSame(db.Person.Where(t => t.Patient.PersonID == patient))
					.Update()
					.Insert()
					.DeleteBySource(t => t.Patient.PersonID == patient);
				merge.Merge();
				patient++;
				merge.Merge();
			}
		}

		[MergeBySourceDataContextSource]
		public void MergeWithDeletePredicate5(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<Child>()
					.FromSame(db.Child.Where(t => t.Parent.ParentID == 2 && t.GrandChildren.Any(g => g.Child.ChildID == 22)))
					//.Update()
					.Insert()
					.DeleteBySource(t => t.Parent.ParentID == 2 && t.GrandChildren.Any(g => g.Child.ChildID == 22))
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
			[Column("NCHARDATATYPE", DataType = DataType.NChar, Length = 20, Configuration = ProviderName.DB2)]
			public string ncharDataType;
			[Column(DataType = DataType.NVarChar, Length = 20)]
			[Column("NVARCHARDATATYPE", DataType = DataType.NVarChar, Length = 20, Configuration = ProviderName.DB2)]
			public string nvarcharDataType;
		}

		// ASE: alltypes table must be fixed
		// DB2: ncharDataType field absent
		[MergeDataContextSource(ProviderName.Sybase, ProviderName.DB2, ProviderName.DB2LUW, ProviderName.DB2zOS)]
		public void MergeChar1(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var id = ConvertTo<int>.From(db.GetTable<AllType>().InsertWithIdentity(() => new AllType
				{
					charDataType = '\x0',
					ncharDataType = "\x0"
				}));

				db.GetTable<AllType>().FromSame(db.GetTable<AllType>().Where(t => t.ID == id)).Update().Insert().Merge();
			}
		}

		// ASE: alltypes table must be fixed
		// DB2: ncharDataType field absent
		[MergeDataContextSource(ProviderName.Sybase, ProviderName.DB2, ProviderName.DB2LUW, ProviderName.DB2zOS)]
		public void MergeChar2(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<AllType>()
					.FromSame(new[]
					{
						new AllType
						{
							ID            = 10,
							charDataType  = '\x0',
							ncharDataType = "\x0"
						}
					}).Update().Insert()
					.Merge();
			}
		}

		// extra test to check MergeChar* fixes (but we really need to implement excessive types tests for all providers)
		// SAP HANA: something wrong with \0 in strings
		// Sybase: AllTypes table must be fixed
		// DB2: something doesn't work
		[MergeDataContextSource(ProviderName.SapHana, ProviderName.Sybase, ProviderName.DB2)]
		public void MergeString(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var lastId = db.GetTable<AllType>().Select(_ => _.ID).Max();

				var rows = db.GetTable<AllType>()
					.FromSame(new[]
					{
						new AllType()
						{
							ID = lastId + 1,
							charDataType = '\x0',
							ncharDataType = "\x0",
							nvarcharDataType = "test\x0it"
						}
					})
					.Insert().Merge();

				Assert.AreEqual(1, rows);

				var row = db.GetTable<AllType>().OrderByDescending(_ => _.ID).Take(1).Single();

				Assert.AreEqual('\0', row.charDataType);
				Assert.AreEqual("\0", row.ncharDataType);
				Assert.AreEqual("test\0it", row.nvarcharDataType);
			}
		}
	}
}
