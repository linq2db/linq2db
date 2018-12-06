﻿using System;
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
	public class OldMergeTests : TestBase
	{
		[Test]
		public void Merge(
			[DataSources(
				false,
				ProviderName.Access,
				ProviderName.Informix,
				ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
				ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest,
				TestProvName.Firebird3,
				ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000, ProviderName.SqlServer2005,
				ProviderName.Sybase, ProviderName.SybaseManaged,
				ProviderName.SapHana)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(db.Types2);
			}
		}

		[Test]
		public void MergeWithEmptySource(
			[DataSources(
				false,
				ProviderName.Access,
				ProviderName.Informix,
				ProviderName.MySql,
				ProviderName.PostgreSQL,
				ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000, ProviderName.SqlServer2005,
				ProviderName.Sybase)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(new Person[] {});
			}
		}

		[Test]
		public void MergeWithDelete(
			[DataSources(
				false,
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Firebird, TestProvName.Firebird3,
				ProviderName.Informix,
				ProviderName.OracleNative, ProviderName.OracleManaged,
				ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
				ProviderName.SapHana,
				ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest,
				ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000, ProviderName.SqlServer2005,
				ProviderName.Sybase, ProviderName.SybaseManaged)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(true, db.Types2);
			}
		}

		[Test]
		public void MergeWithDeletePredicate1(
			[DataSources(
				false,
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Firebird, TestProvName.Firebird3,
				ProviderName.Informix,
				ProviderName.OracleNative, ProviderName.OracleManaged,
				ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
				ProviderName.SapHana,
				ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest,
				ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000, ProviderName.SqlServer2005,
				ProviderName.Sybase, ProviderName.SybaseManaged)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(t => t.ID > 5, db.Types2.Where(t => t.ID > 5));
			}
		}

		[Test]
		public void MergeWithDeletePredicate2(
			[DataSources(
				false,
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Firebird, TestProvName.Firebird3,
				ProviderName.Informix,
				ProviderName.OracleNative, ProviderName.OracleManaged,
				ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
				ProviderName.SapHana,
				ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest,
				ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000, ProviderName.SqlServer2005,
				ProviderName.Sybase, ProviderName.SybaseManaged)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(db.Types2, t => t.ID > 5);
			}
		}

		[Test]
		public async Task MergeWithDeletePredicate2Async(
			[DataSources(
				false,
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Firebird, TestProvName.Firebird3,
				ProviderName.Informix,
				ProviderName.OracleNative, ProviderName.OracleManaged,
				ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
				ProviderName.SapHana,
				ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest,
				ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000, ProviderName.SqlServer2005,
				ProviderName.Sybase, ProviderName.SybaseManaged)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				await db.MergeAsync(db.Types2, t => t.ID > 5);
			}
		}

		[Test]
		public void MergeWithDeletePredicate3(
			[DataSources(
				false,
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Firebird, TestProvName.Firebird3,
				ProviderName.Informix,
				ProviderName.OracleNative, ProviderName.OracleManaged,
				ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
				ProviderName.SapHana,
				ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest,
				ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000, ProviderName.SqlServer2005,
				ProviderName.Sybase, ProviderName.SybaseManaged)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var patient = db.Patient.First();
				db.Merge(db.Person, t => t.Patient == patient);
			}
		}

		[Test]
		public void MergeWithDeletePredicate4(
			[DataSources(
				false,
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Firebird, TestProvName.Firebird3,
				ProviderName.Informix,
				ProviderName.OracleNative, ProviderName.OracleManaged,
				ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
				ProviderName.SapHana,
				ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest,
				ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000, ProviderName.SqlServer2005,
				ProviderName.Sybase, ProviderName.SybaseManaged)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var patient = db.Patient.First().PersonID;
				db.Merge(db.Person, t => t.Patient.PersonID == patient);
				patient++;
				db.Merge(db.Person, t => t.Patient.PersonID == patient);
			}
		}

		[Test]
		public void MergeWithDeletePredicate5(
			[DataSources(
				false,
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Firebird, TestProvName.Firebird3,
				ProviderName.Informix,
				ProviderName.OracleNative, ProviderName.OracleManaged,
				ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
				ProviderName.SapHana,
				ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest,
				ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000, ProviderName.SqlServer2005,
				ProviderName.Sybase, ProviderName.SybaseManaged)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(db.Child, t => t.Parent.ParentID == 2 && t.GrandChildren.Any(g => g.Child.ChildID == 22));
			}
		}

		[Table("AllTypes")]
		class AllType
		{
			[PrimaryKey, Identity] public int ID;
			[Column(DataType = DataType.Char,  Length = 1)]  public char   charDataType;
			[Column(DataType = DataType.NChar, Length = 20)] public string ncharDataType;
		}

		[Test]
		public void MergeChar1(
			[DataSources(
				false,
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Firebird,
				ProviderName.Informix,
				ProviderName.OracleNative, ProviderName.OracleManaged,
				ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
				ProviderName.SapHana,
				ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest,
				ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000, ProviderName.SqlServer2005,
				ProviderName.Sybase, ProviderName.SybaseManaged)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var id = ConvertTo<int>.From(db.GetTable<AllType>().InsertWithIdentity(() => new AllType
				{
					charDataType  = '\x0',
					ncharDataType = "\x0"
				}));

				try
				{
					db.Merge(db.GetTable<AllType>().Where(t => t.ID == id));
				}
				finally
				{
					db.GetTable<AllType>().Delete(t => t.ID == id);
				}
			}
		}

		[Test]
		public void MergeChar2(
			[DataSources(
				false,
				ProviderName.Access,
				ProviderName.DB2,
				ProviderName.Firebird,
				ProviderName.Informix,
				ProviderName.OracleNative, ProviderName.OracleManaged,
				ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57,
				ProviderName.SapHana,
				ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest,
				ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
				ProviderName.SqlCe,
				ProviderName.SqlServer2000, ProviderName.SqlServer2005,
				ProviderName.Sybase, ProviderName.SybaseManaged)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				try
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
				finally
				{
					db.GetTable<AllType>().Delete(t => t.ID == 10);
				}
			}
		}
	}
}
