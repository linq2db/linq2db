using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Update
{
	using System.Linq.Expressions;

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
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.Oracle, ProviderName.MySql,
			ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.SqlServer2005, ProviderName.Sybase)]
		public void MergeWithDeletePredicate1(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Merge(t => t.ID > 5, db.Types2.Where(t => t.ID > 5));
			}
		}

		[Test, DataContextSource(false,
			ProviderName.Access, ProviderName.DB2, ProviderName.Firebird, ProviderName.Informix, ProviderName.Oracle, ProviderName.MySql,
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
	}
}
