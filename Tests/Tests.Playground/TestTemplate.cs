﻿using System.Linq;
using FluentAssertions;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class TestTemplate : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column]              public int     Id    { get; set; }
			[Column(Length = 50)] public string? Value { get; set; }
		}

		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				table.ToArray().Should().BeEmpty();
			}
		}
	}
}
