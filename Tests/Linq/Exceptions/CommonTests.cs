using System;
using System.Collections.Generic;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.SqlQuery;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Exceptions
{
	[TestFixture]
	public class CommonTests : TestBase
	{
		sealed class MyDataConnection : TestDataConnection
		{
			public MyDataConnection(string context) : base(context)
			{
			}

			protected override SqlStatement ProcessQuery(SqlStatement statement, EvaluationContext context)
			{
				if (statement.IsInsert() && statement.RequireInsertClause().Into!.TableName.Name == "Parent")
				{
					var expr =
						statement.RequireInsertClause().Find(static e =>
						{
							if (e.ElementType == QueryElementType.SetExpression)
							{
								var se = (SqlSetExpression)e;
								return ((SqlField)se.Column!).Name == "ParentID";
							}

							return false;
						}) as SqlSetExpression;

					if (expr != null && expr.Expression!.TryEvaluateExpression(context, out var expressionValue))
					{
						var value = ConvertTo<int>.From(expressionValue);

						if (value == 555)
						{
							var tableName = "Parent1";
							var dic       = new Dictionary<IQueryElement,IQueryElement>();

							statement = statement.Convert(new { dic, tableName }, static (v, e) =>
							{
								if (e.ElementType == QueryElementType.SqlTable)
								{
									var oldTable = (SqlTable)e;

									if (oldTable.TableName.Name == "Parent")
									{
										var newTable = new SqlTable(oldTable) { TableName = new (v.Context.tableName) };

										foreach (var field in oldTable.Fields)
											v.Context.dic.Add(field, newTable.FindFieldByMemberName(field.Name) ?? throw new InvalidOperationException());

										return newTable;
									}
								}

								IQueryElement? ex;
								return v.Context.dic.TryGetValue(e, out ex) ? ex : e;
							});
						}
					}

					return statement;
				}

				return statement;
			}
		}

		[Test]
		public void ReplaceTableTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)]
			string context)
		{
			using (var db = new MyDataConnection(context))
			{
				db.BeginTransaction();

				var n = 555;

				var ex = Assert.Throws(
					Is.AssignableTo<Exception>(),
					() =>
						db.Parent.Insert(() => new Parent
						{
							ParentID = n,
							Value1   = n
						}),
					"Invalid object name 'Parent1'.")!;
				Assert.That(ex.GetType().Name, Is.EqualTo("SqlException"));

				ex = Assert.Throws(
					Is.AssignableTo<Exception>(),
					() =>
						db.Parent.Insert(() => new Parent
						{
							ParentID = n,
							Value1   = n
						}),
					"Invalid object name 'Parent1'.")!;
				Assert.That(ex.GetType().Name, Is.EqualTo("SqlException"));

				db.Parent.Delete(p => p.ParentID == n);
			}
		}
	}
}
