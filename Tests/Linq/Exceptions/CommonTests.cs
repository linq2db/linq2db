﻿using System;
using System.Collections.Generic;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Exceptions
{
	using Model;

	[TestFixture]
	public class CommonTests : TestBase
	{
		class MyDataConnection : TestDataConnection
		{
			public MyDataConnection(string context) : base(context)
			{
			}

			protected override SqlStatement ProcessQuery(SqlStatement statement)
			{
				if (statement.IsInsert() && statement.RequireInsertClause().Into!.Name == "Parent")
				{
					var expr =
						new QueryVisitor().Find(statement.RequireInsertClause(), e =>
						{
							if (e.ElementType == QueryElementType.SetExpression)
							{
								var se = (SqlSetExpression)e;
								return ((SqlField)se.Column).Name == "ParentID";
							}

							return false;
						}) as SqlSetExpression;

					if (expr != null)
					{
						var value = ConvertTo<int>.From(((IValueContainer)expr.Expression!).Value);

						if (value == 555)
						{
							var tableName = "Parent1";
							var dic       = new Dictionary<IQueryElement,IQueryElement>();

							statement = ConvertVisitor.Convert(statement, (v, e) =>
							{
								if (e.ElementType == QueryElementType.SqlTable)
								{
									var oldTable = (SqlTable)e;

									if (oldTable.Name == "Parent")
									{
										var newTable = new SqlTable(oldTable) { Name = tableName, PhysicalName = tableName };

										foreach (var field in oldTable.Fields)
											dic.Add(field, newTable[field.Name] ?? throw new InvalidOperationException());

										return newTable;
									}
								}

								IQueryElement? ex;
								return dic.TryGetValue(e, out ex) ? ex : e;
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
					"Invalid object name 'Parent1'.");
				Assert.True(ex.GetType().Name == "SqlException");

				ex = Assert.Throws(
					Is.AssignableTo<Exception>(),
					() =>
						db.Parent.Insert(() => new Parent
						{
							ParentID = n,
							Value1   = n
						}),
					"Invalid object name 'Parent1'.");
				Assert.True(ex.GetType().Name == "SqlException");

				db.Parent.Delete(p => p.ParentID == n);
			}
		}
	}
}
