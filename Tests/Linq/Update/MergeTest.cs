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

	[TestFixture]
	public class MergeTest : TestBase
	{
		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.Informix, ProviderName.MySql, ProviderName.PostgreSQL, ProviderName.SQLite,
			ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
		public void Merge(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(db.Types2);
			}
		}

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.Informix, ProviderName.MySql, ProviderName.PostgreSQL, ProviderName.SQLite,
			ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
		public void MergeWithEmptySource(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(new Person[] {});
			}
		}

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.Oracle, ProviderName.MySql,
			ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
		public void MergeWithDelete(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(true, db.Types2);
			}
		}

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.Oracle, ProviderName.MySql, ProviderName.SapHana,
			ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
		public void MergeWithDeletePredicate1(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(t => t.ID > 5, db.Types2.Where(t => t.ID > 5));
			}
		}

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.Oracle, ProviderName.MySql, ProviderName.SapHana,
			ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
		public void MergeWithDeletePredicate2(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(db.Types2, t => t.ID > 5);
			}
		}

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.Oracle, ProviderName.MySql,
			ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
		public void MergeWithDeletePredicate3(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var patient = db.Patient.First();
				db.Merge(db.Person, t => t.Patient == patient);
			}
		}

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.Oracle, ProviderName.MySql,
			ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
		public void MergeWithDeletePredicate4(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var patient = db.Patient.First().PersonID;
				db.Merge(db.Person, t => t.Patient.PersonID == patient);
				patient++;
				db.Merge(db.Person, t => t.Patient.PersonID == patient);
			}
		}

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.Oracle, ProviderName.MySql,
			ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
		public void MergeWithDeletePredicate5(string context)
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
			[Column(DataType = DataType.Char,  Length = 1)]   public char   charDataType;
			[Column(DataType = DataType.NChar, Length = 20)]  public string ncharDataType;
		}

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.Oracle, ProviderName.MySql,
			ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
		public void MergeChar1(string context)
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

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.Oracle, ProviderName.MySql,
			ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
		public void MergeChar2(string context)
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
