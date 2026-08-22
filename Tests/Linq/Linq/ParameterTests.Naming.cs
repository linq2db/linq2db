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
		// nearest member access inside it is used - but only across calls that hand back a value the target
		// already holds. A call that computes a new value is named exactly as it was before, which may be
		// the column name or the generic fallback depending on what the call site carries.
		//
		// Assertions go against DataParameter.Name rather than the SQL text: the name carries no provider
		// prefix there, and a substring check over the SQL would match "@p" inside "@price".

		[Test]
		public void ParameterName_FromArrayBeingIndexed([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var values = new[] { "str", "str1" };

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == values[0] || t.String3 == values[1])
				.ToSqlQuery();

			sql.Parameters.Select(p => p.Name).ShouldBe(["values", "values_1"]);
		}

		[Test]
		public void ParameterName_FromNullableTarget([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			int? value = 1;

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == value.GetValueOrDefault())
				.ToSqlQuery();

			sql.Parameters.Select(p => p.Name).ShouldBe(["value"]);
		}

		[Test]
		public void ParameterName_NullableTargetWithExplicitDefault([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			int? value = null;

			// This overload can return the argument rather than the target's value - naming the parameter
			// after `value` would describe something it does not hold, so the target's name is not used.
			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == value.GetValueOrDefault(7))
				.ToSqlQuery();

			// Checked as a prefix rather than an exact sequence: the target's name must not appear at all,
			// including as "value_1" after uniquification or alongside a parameter added later. The
			// non-empty check keeps ShouldAllBe from passing vacuously if the value stops being a parameter.
			sql.Parameters.ShouldNotBeEmpty();
			sql.Parameters.Select(p => p.Name).ShouldAllBe(n => !n!.StartsWith("value", StringComparison.Ordinal));
		}

		[Test]
		public void ParameterName_FromIndexedCollection([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var names = new List<string> { "str" };

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == names[0])
				.ToSqlQuery();

			sql.Parameters.Select(p => p.Name).ShouldBe(["names"]);
		}

		[Test]
		public void ParameterName_ComputingCallIsNotNamedAfterTarget([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var text = "st";

			// Trim returns a new value, so naming the parameter after `text` would describe the wrong thing.
			// Such calls are left exactly as they were before source-based naming existed - here that means
			// the generic fallback, since this call site carries no column descriptor to name after either.
			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == text.Trim())
				.ToSqlQuery();

			sql.Parameters.Select(p => p.Name).ShouldBe(["p"]);
		}
	}
}
