using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue825Tests : TestBase
	{
		[Table(Name = "Child")]
		public class Child825
		{
			[PrimaryKey, Identity, Column("ChildID")]
			public int Id { get; set; }

			[Column("ParentID"), NotNull]
			public int ParentId { get; set; }

			[Association(ThisKey = "ParentId", OtherKey = "Id", CanBeNull = false)]
			public Parent825 Parent { get; set; }
		}

		[Table(Name = "Parent")]
		public class Parent825
		{
			[PrimaryKey, Identity, Column("ParentID")]
			public int Id { get; set; }

			[Association(ThisKey = "Id", OtherKey = "ParentId", CanBeNull = true, IsBackReference = true)]
			public IList<ParentPermission> ParentPermissions { get; set; }

			[Association(ThisKey = "Id", OtherKey = "ParentId", CanBeNull = true, IsBackReference = true)]
			public IList<Child825> Childs { get; set; }
		}

		[Table(Name = "GrandChild")]
		public class ParentPermission
		{
			[PrimaryKey, Identity, Column("GrandChildID")]
			public int Id { get; set; }

			[Column("ParentID"), NotNull]
			public int ParentId { get; set; }

			[Column("ChildID"), NotNull]
			public int UserId { get; set; }
		}

		[Test, DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var userId = 32;
				var childId = 32;

				//Configuration.Linq.OptimizeJoins = false;

				using (new AllowMultipleQuery())
				{
					var query = db.GetTable<Parent825>()
						.Where(p => p.ParentPermissions.Any(permission => permission.UserId == userId))
						.SelectMany(parent => parent.Childs)
						.Where(child => child.Id == childId)
						.Select(child => child.Parent);

					var result = query.ToList();

					Assert.AreEqual(1, result.Count);
					Assert.AreEqual(3, result[0].Id);
				}
			}
		}
	}
}
