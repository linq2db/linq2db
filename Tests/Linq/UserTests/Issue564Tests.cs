﻿using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue564Tests : TestBase
	{
		[Table("Parent564")]
		[InheritanceMapping(Code = "Child564A", Type = typeof(Child564A))]
		[InheritanceMapping(Code = "Child564B", Type = typeof(Child564B))]
		public class Parent564
		{
			[PrimaryKey, Identity]
			public int Id;

			[Column(IsDiscriminator = true)]
			public string? Type;
		}

		public class Child564A : Parent564
		{
			public Child564A()
			{
				Type = "Child564A";
			}

			[Column(Length = 20)]
			public string? StringValue { get; set; }
		}

		public class Child564B : Parent564
		{
			public Child564B()
			{
				Type = "Child564B";
			}

			[Column(CanBeNull = true)]
			public int IntValue { get; set; }
		}

		[Test]
		public void Test([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable<Parent564>())
			{
				db.Insert(new Child564A() {StringValue = "SomeValue"});
				db.Insert(new Child564B() {IntValue    = 911});

				Assert.AreEqual(2, db.GetTable<Parent564>().Count());
			}
		}
	}
}
