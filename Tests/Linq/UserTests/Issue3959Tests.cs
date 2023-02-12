﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Extensions;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3959Tests : TestBase
	{
		// not mixing props and fields because GetMembers()
		// will return all props (derived first) and then all fields (derived first)
		public class I3959_Base
		{
			public int Member1;
			public int Member2;
			public int Member3;
		}

		public class I3959_Derived : I3959_Base
		{
			public int MemberDerived1;
			public new int Member2;
			public int MemberDerived3;
		}

		[Test]
		public void TestMemberOrdering()
		{
			var type = typeof(I3959_Derived);

			var members = type.GetPublicInstanceValueMembers();

			//Console.WriteLine("Members: {0}", string.Join(", ", members.Select(m => m.Name)));

			// Test returned array
			Assert.IsNotNull(members);
			Assert.IsTrue(members.Length == 5, "Expected 5 returned members, found {0}.", members.Length);

			var expected = String.Join(", ", new []
			{ 
				$"{nameof(I3959_Base)}.{nameof(I3959_Base.Member1)}",
				$"{nameof(I3959_Base)}.{nameof(I3959_Base.Member3)}",
				$"{nameof(I3959_Derived)}.{nameof(I3959_Derived.MemberDerived1)}",
				$"{nameof(I3959_Derived)}.{nameof(I3959_Derived.Member2)}",
				$"{nameof(I3959_Derived)}.{nameof(I3959_Derived.MemberDerived3)}",
			});

			var actual = String.Join(", ", members.Select(m => $"{m.DeclaringType!.Name}.{m.Name}"));

			Assert.AreEqual(expected, actual);
		}
	}
}
