using System;
using System.Collections.Generic;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Exceptions
{
	using Model;

	[TestFixture]
	public class Common : TestBase
	{
		class MyDataConnection : TestDataConnection
		{
			public MyDataConnection(string context) : base(context)
			{
			}

			protected override SelectQuery ProcessQuery(SelectQuery selectQuery)
			{
				if (selectQuery.IsInsert && selectQuery.Insert.Into.Name == "Parent")
				{
					var expr =
						new QueryVisitor().Find(selectQuery.Insert, e =>
						{
							if (e.ElementType == QueryElementType.SetExpression)
							{
								var se = (SelectQuery.SetExpression)e;
								return ((SqlField)se.Column).Name == "ParentID";
							}

							return false;
						}) as SelectQuery.SetExpression;

					if (expr != null)
					{
						var value = ConvertTo<int>.From(((IValueContainer)expr.Expression).Value);

						if (value == 555)
						{
							var tableName = "Parent1";
							var dic       = new Dictionary<IQueryElement,IQueryElement>();

							selectQuery = new QueryVisitor().Convert(selectQuery, e =>
							{
								if (e.ElementType == QueryElementType.SqlTable)
								{
									var oldTable = (SqlTable)e;

									if (oldTable.Name == "Parent")
									{
										var newTable = new SqlTable(oldTable) { Name = tableName, PhysicalName = tableName };

										foreach (var field in oldTable.Fields.Values)
											dic.Add(field, newTable.Fields[field.Name]);

										return newTable;
									}
								}

								IQueryElement ex;
								return dic.TryGetValue(e, out ex) ? ex : null;
							});
						}
					}
				}

				return selectQuery;
			}
		}

		[Test, ExpectedException(typeof(System.Data.SqlClient.SqlException), ExpectedMessage = "Invalid object name 'Parent1'.")]
		public void ReplaceTableTest([IncludeDataContexts(ProviderName.SqlServer2008)] string context)
		{
			using (var db = new MyDataConnection(context))
			{
				db.BeginTransaction();

				var n = 555;

				db.Parent.Insert(() => new Parent
				{
					ParentID = n,
					Value1   = n
				});

				db.Parent.Delete(p => p.ParentID == n);
			}
		}
	}
}
