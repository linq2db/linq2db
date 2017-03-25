using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue175Tests : TestBase
	{
		new public class Parent
		{
			public int? ParentID;
		}

		new public class Child
		{
			public int? ParentID;
			public int? ChildID;
		}

		[Test, DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from c in db.GetTable<Child>()
						join p in db.GetTable<Parent>() on c.ParentID equals p.ParentID
						select c;

				Assert.IsNotEmpty(q);
			}
		}
	}
}
