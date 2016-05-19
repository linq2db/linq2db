﻿using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[Table(Name = "GrandChild")]
	public class GrandChild
	{
		[Column] public int ChildID { get; set; }
	}

	[Table(Name = "Child")]
	public class Child
	{
		[Column] public int ParentID { get; set; }
		[Column] public int ChildID  { get; set; }

		[Association(ThisKey = "ChildID", OtherKey = "ChildID", CanBeNull = false)]
		public List<GrandChild> GrandChildren { get; set; }
	}

	[Table(Name = "Parent")]
	public class Parent
	{
		[Identity, PrimaryKey(1)]
		public int ParentID { get; set; }

		[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
		public List<Child> Children { get; set; }
	}

	[TestFixture]
	public class SelectManyDeleteTests : TestBase
	{
		[Test, DataContextSource(
			ProviderName.Access, ProviderName.DB2, ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, ProviderName.SqlCe, ProviderName.SQLite, ProviderName.Firebird, ProviderName.SapHana
			)]
		public void Test(string context)
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

