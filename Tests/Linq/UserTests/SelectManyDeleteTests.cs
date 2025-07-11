using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class SelectManyDeleteTests : TestBase
	{
		[Table(Name = "GrandChild")]
		new sealed class GrandChild
		{
			[Column] public int ChildID { get; set; }
		}

		[Table(Name = "Child")]
		new sealed class Child
		{
			[Column] public int ParentID { get; set; }
			[Column] public int ChildID  { get; set; }

			[Association(ThisKey = "ChildID", OtherKey = "ChildID", CanBeNull = false)]
			public List<GrandChild> GrandChildren { get; set; } = null!;
		}

		[Table(Name = "Parent")]
		new sealed class Parent
		{
			[Identity, PrimaryKey(1)]
			public int ParentID { get; set; }

			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public List<Child> Children { get; set; } = null!;
		}

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.ClickHouse.Error_CorrelatedDelete)]
		[Test]
		public void Test([DataSources] string context)
		{
			var harnessIds = new int[2];

			using (var db = GetDataContext(context))
			{
				db.GetTable<Parent>()
					.Where     (x => harnessIds.Contains(x.ParentID))
					.SelectMany(x => x.Children)
					.SelectMany(x => x.GrandChildren)
					.Delete();
			}
		}
	}
}

