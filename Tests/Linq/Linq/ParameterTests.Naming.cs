using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class ParameterTests
	{
		// A parameter carries a value, so it reads better named after where that value comes from than
		// after the column it is compared against. When the expression is not itself a member access, the
		// nearest member access inside it is used - otherwise the name falls back to the column or to "p".

		[Test]
		public void ParameterName_FromArrayBeingIndexed([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var values = new[] { "str", "str1" };

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == values[0] || t.String3 == values[1])
				.ToSqlQuery();

			sql.Parameters.Count.ShouldBe(2);
			sql.Sql.ShouldContain("@values");
			sql.Sql.ShouldNotContain("@p");
		}

		[Test]
		public void ParameterName_FromMethodCallTarget([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			int? value = 1;

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == value.GetValueOrDefault())
				.ToSqlQuery();

			sql.Sql.ShouldContain("@value");
			sql.Sql.ShouldNotContain("@p");
		}

		[Test]
		public void ParameterName_FromIndexedCollection([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var names = new List<string> { "str" };

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == names[0])
				.ToSqlQuery();

			sql.Sql.ShouldContain("@names");
			sql.Sql.ShouldNotContain("@p");
		}
	}
}
