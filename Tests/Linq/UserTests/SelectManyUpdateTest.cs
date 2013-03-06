using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class SelectManyUpdateTest : TestBase
	{
		public new class Child
		{
			[Identity, PrimaryKey(1)] public int  ParentID { get; set; }
			[Nullable]                public int? ChildID { get; set; }

			[Association(ThisKey = "ParentID", OtherKey = "ChildID", CanBeNull = true)]
			public List<Child> Children { get; set; }
		}

		public new class Parent
		{
			[Identity, PrimaryKey(1)] public int  ParentID { get; set; }
			[Nullable]                public int? Value1   { get; set; }

			[Association(ThisKey = "ParentID", OtherKey = "Value1", CanBeNull = true)]
			public List<Parent> Values { get; set; }

			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public List<Child> Children { get; set; }
		}

		[Test]
		public void Test1([DataContexts(ProviderName.Access, ProviderName.Informix, ProviderName.MySql)] string context)
		{
			var harnessIds = new int[2];

			using (var db = GetDataContext(context))
				db.GetTable<Parent>()
					.Where     (x => harnessIds.Contains(x.ParentID))
					.SelectMany(x => x.Values)
					.Set       (x => x.Value1, (long?)null)
					.Update();
		}

		[Test]
		public void Test2([DataContexts(ProviderName.Access, ProviderName.Informix, ProviderName.MySql)] string context)
		{
			var harnessIds = new int[0];

			using (var db = GetDataContext(context))
				db.GetTable<Parent>()
					.Where     (x => harnessIds.Contains(x.ParentID))
					.SelectMany(x => x.Children)
					.SelectMany(x => x.Children)
					.Set       (x => x.ChildID, 10)
					.Update();
		}
	}
}
