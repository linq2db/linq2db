using System;
using System.Linq;
using LinqToDB;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.UserTests
{
	class MyItemBuilder : Sql.IExtensionCallBuilder
	{
		public void Build(Sql.ISqExtensionBuilder builder)
		{
			var values = builder.GetValue<System.Collections.IEnumerable>("values");
			builder.Query.IsParameterDependent = true;

			foreach (var value in values)
			{
				var param = new SqlParameter(value?.GetType() ?? typeof(object), "p", value);
				builder.AddParameter("values", param);
			}
		}
	}

	public static class MySqlExtensions
	{
		[Sql.Extension("{field} IN ({values, ', '})", IsPredicate = true, BuilderType = typeof(MyItemBuilder))]
		public static bool In<T>(this Sql.ISqlExtension ext, [ExprParameter] T field, params T[] values)
		{
			throw new NotImplementedException();
		}
	}

	public class Issue973Tests : TestBase
	{
		[Test, DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var param = 4;
				var resultQuery =
						from o in db.Parent
						where Sql.Ext.In(o.ParentID, 1, 2, 3, (int?)null) || o.ParentID == param
						select o;

				var str = resultQuery.ToString();
				Console.WriteLine(str);
				var zz = resultQuery.ToArray();
			}
		}

	}
}
