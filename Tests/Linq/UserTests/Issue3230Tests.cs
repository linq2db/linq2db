using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3230Tests : TestBase
	{
		[Table("GrandParent_3230")]
		public class GrandParent
		{
			[Column] public int ID { get; set; }
		}

		[Table("Parent_3230")]
		public new class Parent
		{
			[Column]                                                  public int          ID            { get; set; }
			[Column]                                                  public int          GrandParentID { get; set; }
			[Association(ThisKey = "GrandParentID", OtherKey = "ID")] public GrandParent? GrandParent   { get; set; }
		}

		[Table("Child_3230")]
		public new class Child
		{
			[Column]                                             public int     ID       { get; set; }
			[Column]                                             public int     ParentID { get; set; }
			[Association(ThisKey = "ParentID", OtherKey = "ID")] public Parent? Parent   { get; set; }
		}

		public class ChildViewModel : Child
		{
		}

		[Test]
		public void InheritedAssociation([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new GrandParent[]{new(){ID = 1}}))
			using (db.CreateLocalTable(new Parent[]{new(){ID = 1, GrandParentID = 1}}))
			using (db.CreateLocalTable(new Child[]{new(){ID = 1, ParentID = 1}}))
			{
				var items1 = db.GetTable<Child>()
					.LoadWith(p => p.Parent)
					.LoadWith(p => p.Parent!.GrandParent)
					.ToList();

				items1.Count.ShouldBe(1);
				items1[0].Parent?.GrandParent.ShouldNotBeNull();

				var items2 = db.GetTable<ChildViewModel>()
					.LoadWith(p => p.Parent)
					.LoadWith(p => p.Parent!.GrandParent)
					.ToList();				

				items2.Count.ShouldBe(1);
				items2[0].Parent?.GrandParent.ShouldNotBeNull();
			}
		}
	}
}
