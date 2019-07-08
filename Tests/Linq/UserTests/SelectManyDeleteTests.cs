﻿using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class SelectManyDeleteTests : TestBase
	{
		[Table(Name = "GrandChild")]
		new class GrandChild
		{
			[Column] public int ChildID { get; set; }
		}

		[Table(Name = "Child")]
		new class Child
		{
			[Column] public int ParentID { get; set; }
			[Column] public int ChildID  { get; set; }

			[Association(ThisKey = "ChildID", OtherKey = "ChildID", CanBeNull = false)]
			public List<GrandChild> GrandChildren { get; set; }
		}

		[Table(Name = "Parent")]
		new class Parent
		{
			[Identity, PrimaryKey(1)]
			public int ParentID { get; set; }

			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public List<Child> Children { get; set; }
		}

		[Test]
		public void Test([DataSources(
			ProviderName.Access,
			ProviderName.DB2,
			ProviderName.Informix,
			TestProvName.AllOracle,
			ProviderName.SqlCe,
			TestProvName.AllSQLite,
			ProviderName.SapHana)]
			string context)
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

