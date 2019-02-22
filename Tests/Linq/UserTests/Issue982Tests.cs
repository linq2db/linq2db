using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;
using NUnit.Framework;
using System;
using System.Linq;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue982Tests : TestBase
	{
		private class Issue982FirebirdSqlOptimizer : FirebirdSqlOptimizer
		{
			public Issue982FirebirdSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
			{
			}

			public override SqlStatement Finalize(SqlStatement statement)
			{
				statement = base.Finalize(statement);

				AddConditions(statement);

				return statement;
			}

			private object GetMaxValue(DataType type)
			{
				switch (type)
				{
					case DataType.Double:
						return double.MaxValue;
					case DataType.Int32:
						return int.MaxValue;
					case DataType.Int16:
					case DataType.Boolean:
						return short.MaxValue;
					case DataType.Int64:
						return long.MaxValue;
					case DataType.Char:
					case DataType.NChar:
					case DataType.VarChar:
					case DataType.NVarChar:
					case DataType.Guid:
						return string.Empty;
					case DataType.Date:
						return new DateTime(9999, 09, 09);
					case DataType.DateTime:
					case DataType.DateTime2:
					case DataType.Time:
						return new DateTime(9999, 09, 09, 09, 09, 09);
					case DataType.Decimal:
					case DataType.VarBinary:
						return null;
					default:
						throw new InvalidOperationException($"Unsupported type: {type}");
				}
			}

			private void AddConditions(SqlWhereClause where, ISqlTableSource table)
			{
				var keys = table.GetKeys(true);
				if (keys == null)
					return;

				foreach (var key in keys.OfType<SqlField>())
				{
					var maxValue = GetMaxValue(key.DataType);
					if (maxValue == null)
						continue;

					var cond = new SqlSearchCondition();

					cond = cond.Expr(key).IsNull.Or;

					if (maxValue is string)
						cond.Expr(key).GreaterOrEqual.Expr(new SqlValue(maxValue));
					else
						cond.Expr(key).LessOrEqual.Expr(new SqlValue(maxValue));

					where.ConcatSearchCondition(cond);

					// only one field is enough
					break;
				}
			}

			private void AddConditions(SqlStatement statement)
			{
				statement.WalkQueries(query =>
				{
					new QueryVisitor().Visit(query, e =>
					{
						if (e.ElementType != QueryElementType.SqlQuery)
							return;

						var q = (SelectQuery)e;

						foreach (var source in q.From.Tables)
						{
							if (source.Joins.Any())
								AddConditions(q.Select.Where, source);

							foreach (var join in source.Joins)
								AddConditions(q.Select.Where, join.Table);
						}
					});

					return query;
				});
			}
		}

		[Test]
		public void Test([IncludeDataSources(ProviderName.Firebird, TestProvName.Firebird3)] string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);
			var oldProvider      = DataConnection.GetDataProvider(context);

			try
			{
				DataConnection.AddConfiguration(
					context,
					connectionString,
					new FirebirdDataProvider(new Issue982FirebirdSqlOptimizer(oldProvider.SqlProviderFlags)));

				using (var db = GetDataContext(context))
				{
					var query = from p in db.Parent
								from c in db.Child.InnerJoin(c => c.ParentID == p.ParentID)
								from cg in (
									from cc in db.Child
									group cc by cc.ChildID
									into g
									select g.Key
								).InnerJoin(cg => c.ChildID == cg)
								where p.ParentID > 1 || p.ParentID > 0
								select new
								{
									p,
									c
								};

					var str = query.ToString();
					Assert.True(str.Contains("2147483647"));
					var _ = query.ToArray();
				}
			}
			finally
			{
				// restore
				DataConnection.AddConfiguration(context, connectionString, oldProvider);
			}
		}
	}
}
