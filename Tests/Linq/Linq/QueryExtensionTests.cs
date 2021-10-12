using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class QueryExtensionTests : TestBase
	{
		[Test]
		public void EmptyTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			_ = db.Parent.Empty().ToList();
		}

		[Test]
		public void EmptyTest2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			_ =
			(
				from p in db.Parent.Empty()
				from c in db.Child.Empty()
				where p.ParentID == c.ParentID
				select new { p, c }
			)
			.ToList();
		}
	}

	public static class QueryExtensions
	{
		[Sql.QueryExtensionAttribute]
		public static ITable<T> Empty<T>(this ITable<T> table)
			where T : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Empty, table),
				table.Expression);

			return table;
		}
	}
}
