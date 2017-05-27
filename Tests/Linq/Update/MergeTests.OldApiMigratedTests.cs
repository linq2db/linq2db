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
		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.Informix, ProviderName.MySql, ProviderName.PostgreSQL, ProviderName.SQLite, TestProvName.SQLiteMs,
			ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase, TestProvName.MariaDB, TestProvName.MySql57)]
		public void Merge(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.GetTable<LinqDataTypes2>().FromSame(db.Types2)
					.Update().Insert()
					.Merge();
			}
		}

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.Informix, ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57, ProviderName.PostgreSQL, ProviderName.SQLite, TestProvName.SQLiteMs,
			ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
		public void MergeWithEmptySource(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.GetTable<Person>().FromSame(new Person[] { })
					.Update().Insert()
					.Merge();
			}
		}

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
			ProviderName.PostgreSQL, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
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

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57, ProviderName.SapHana,
			ProviderName.PostgreSQL, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
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

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
			ProviderName.PostgreSQL, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
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

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
			ProviderName.PostgreSQL, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
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

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
			ProviderName.PostgreSQL, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
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

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
			ProviderName.PostgreSQL, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
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

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
			ProviderName.PostgreSQL, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
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
