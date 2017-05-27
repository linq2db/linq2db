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

		[Table("AllTypes")]
		class AllType
		{
			[PrimaryKey, Identity]
			public int ID;
			[Column(DataType = DataType.Char, Length = 1)]
			public char charDataType;
			[Column(DataType = DataType.NChar, Length = 20)]
			public string ncharDataType;
		}

		// ASE: alltypes table must be fixed
		// DB2: ncharDataType field absent
		[MergeDataContextSource(ProviderName.Sybase, ProviderName.DB2, ProviderName.DB2LUW, ProviderName.DB2zOS)]
		public void MergeChar1(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var id = ConvertTo<int>.From(db.GetTable<AllType>().InsertWithIdentity(() => new AllType
				{
					charDataType = '\x0',
					ncharDataType = "\x0"
				}));

				try
				{
					db.GetTable<AllType>().FromSame(db.GetTable<AllType>().Where(t => t.ID == id)).Update().Insert().Merge();
				}
				finally
				{
					db.GetTable<AllType>().Delete(t => t.ID == id);
				}
			}
		}

		// ASE: alltypes table must be fixed
		// DB2: ncharDataType field absent
		[MergeDataContextSource(ProviderName.Sybase, ProviderName.DB2, ProviderName.DB2LUW, ProviderName.DB2zOS)]
		public void MergeChar2(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				try
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
				finally
				{
					db.GetTable<AllType>().Delete(t => t.ID == 10);
				}
			}
		}
	}
}
