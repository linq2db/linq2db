using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class PostgreSQLSqlOptimizer : BasicSqlOptimizer
	{
		public PostgreSQLSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement Finalize(SqlStatement statement)
		{
			CheckAliases(statement, int.MaxValue);

			statement = base.Finalize(statement);

			switch (statement.QueryType)
			{
				case QueryType.Delete : return GetAlternativeDelete((SqlDeleteStatement)statement);
				case QueryType.Update : return CorrectUpdate((SqlUpdateStatement)statement);
				default               : return statement;
			}
		}

		SqlTableSource GetMainTableSource(SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count > 0 && selectQuery.From.Tables[0] is SqlTableSource tableSource)
				return tableSource;
			return null;
		}

		SqlStatement CorrectUpdate(SqlUpdateStatement statement)
		{
			// removing joins
			statement.SelectQuery.TransformInnerJoinsToWhere();

			var tableSource = GetMainTableSource(statement.SelectQuery);
			if (tableSource == null)
				throw new LinqToDBException("Invalid query");

			// envelop query
			if (tableSource.Source is SqlTable currentTable && !statement.SelectQuery.IsSimple)
			{
				var newQuery = new SelectQuery();
				newQuery.Select.IsDistinct = false;
				newQuery.ParentSelect = statement.SelectQuery.ParentSelect;
				newQuery.Select.From.Table(statement.SelectQuery);

				for (var i = 0; i < statement.Update.Items.Count; i++)
				{
					var item = statement.Update.Items[i];

					if (null != QueryVisitor.Find(item.Expression, e => e is SqlField))
					{
						var idx = statement.SelectQuery.Select.Add(item.Expression);
						item.Expression = statement.SelectQuery.Select.Columns[idx];
					}
				}

				statement.SelectQuery = newQuery;
				var alias = tableSource.Alias;
				tableSource = GetMainTableSource(newQuery);
				tableSource.Alias = alias;
			}


			SqlTable tableToUpdate = statement.Update.Table;
			SqlTable tableToCompare = null;
			SelectQuery queryToCompare = null;


			switch (tableSource.Source)
			{
				case SqlTable table:
					{
						if (tableSource.Joins.Count == 0)
						{
							// remove table from FROM clause
							statement.SelectQuery.From.Tables.RemoveAt(0);
							if (tableToUpdate != null && tableToUpdate != table)
							{
								statement.Walk(false, e =>
								{
									if (e is SqlField field && field.Table == tableToUpdate)
									{
										return table.Fields[field.Name];
									}

									return e;
								});
							}
							tableToUpdate = table;
						}
						else
						{
							if (tableToUpdate == null)
							{
								tableToUpdate = QueryHelper.EnumerateJoinedSources(statement.SelectQuery)
									.Select(ts => (ts as SqlTableSource)?.Source as SqlTable)
									.FirstOrDefault(t => t != null);
							}

							if (tableToUpdate == null)
								throw new LinqToDBException("Can no decide which table to update");

							tableToCompare = QueryHelper.EnumerateJoinedSources(statement.SelectQuery)
								.Select(ts => (ts as SqlTableSource)?.Source as SqlTable)
								.FirstOrDefault(t => t != null && QueryHelper.IsEqualTables(t, tableToUpdate));

							if (ReferenceEquals(tableToUpdate, tableToCompare))
							{
								// we have to create clone
								tableToUpdate = tableToUpdate.Clone();

								for (var i = 0; i < statement.Update.Items.Count; i++)
								{
									var item = statement.Update.Items[i];
									var newItem = new QueryVisitor().Convert(item, e =>
									{
										if (e is SqlField field && field.Table == tableToCompare)
										{
											return tableToUpdate.Fields[field.Name];
										}

										return e;
									});

									statement.Update.Items[i] = newItem;
									//item.Column = tableToUpdate.Fields[setField.Name];
								}

								//var setField = QueryHelper.GetUnderlyingField(item.Column);
								//if (setField == null)
								//	throw new LinqToDBException(
								//		$"Unexpected element in setter expression: {item.Column}");

							}
						}

						break;
					}
				case SelectQuery query:
					{
						if (tableToUpdate == null)
						{
							tableToUpdate = QueryHelper.EnumerateJoinedSources(query)
								.Select(ts => (ts as SqlTableSource)?.Source as SqlTable)
								.FirstOrDefault(t => t != null);

							if (tableToUpdate == null)
								throw new LinqToDBException("Can no decide which table to update");

							tableToUpdate = tableToUpdate.Clone();

							foreach (var item in statement.Update.Items)
							{
								var setField = QueryHelper.GetUnderlyingField(item.Column);
								if (setField == null)
									throw new LinqToDBException($"Unexpected element in setter expression: {item.Column}");

								item.Column = tableToUpdate.Fields[setField.Name];
							}

						}

						// return first matched table
						tableToCompare = QueryHelper.EnumerateJoinedSources(query)
							.Select(ts => (ts as SqlTableSource)?.Source as SqlTable)
							.FirstOrDefault(t => t != null && QueryHelper.IsEqualTables(t, tableToUpdate));

						if (tableToCompare == null)
							throw new LinqToDBException("Query can't be translated to UPDATE Statement.");

						queryToCompare = query;

						break;
					}
			}

			if (statement.SelectQuery.From.Tables.Count > 0 && tableToCompare != null)
			{

				var keys1 = tableToUpdate.GetKeys(true);

				if (keys1.Count == 0)
					throw new LinqToDBException($"Table {tableToUpdate.Name} do not have primary key. Update transformation is not availaible.");

				IList<ISqlExpression> keys2;

				if (queryToCompare != null)
				{
					var tableFields = tableToCompare.GetKeys(true);
					keys2 = new List<ISqlExpression>();

					foreach (var field in tableFields)
					{
						var column = queryToCompare.Select.Columns.FirstOrDefault(c => field.Equals(QueryHelper.GetUnderlyingField(c)));
						if (column == null)
						{
							column = queryToCompare.Select.Columns[queryToCompare.Select.AddNew(field)];
						}
						keys2.Add(column);
					}
				}
				else
				{
					keys2 = tableToCompare.GetKeys(true);
				}

				// consider to create additional where

				for (int i = 0; i < keys1.Count; i++)
				{
					statement.SelectQuery.Where
						.Expr(keys1[i]).Equal.Expr(keys2[i]);
				}
			}

			statement.Update.Table = tableToUpdate;
			statement.SetAliases();

			return statement;
		}

		SqlStatement CorrectUpdateOld(SqlUpdateStatement statement)
		{
			if (statement.SelectQuery.From.Tables[0] is SqlTableSource tableSource)
			{
				while (tableSource.Joins.Count > 0 && tableSource.Joins[0].JoinType == JoinType.Inner)
				{
					// consider to remove join and simplify query
					var join = tableSource.Joins[0];
					statement.SelectQuery.Where.ConcatSearchCondition(join.Condition);
					statement.SelectQuery.From.Tables.Add(join.Table);
					tableSource.Joins.RemoveAt(0);
				}

				if (tableSource.Joins.Count == 0)
				{
					// remove table from FROM clause
					statement.SelectQuery.From.Tables.RemoveAt(0);
					statement.Update.Table = QueryHelper.EnumerateJoinedSources(statement.SelectQuery)
						.Select(ts => (ts as SqlTableSource)?.Source as SqlTable)
						.FirstOrDefault(t => t != null);
				}
				else
				{
					SqlTable tableToUpdate;
					SqlTable tableToCompare;

					if (statement.Update.Table != null)
					{
						tableToUpdate = statement.Update.Table;

						// return first matched table
						tableToCompare = QueryHelper.EnumerateJoinedSources(statement.SelectQuery)
							.Select(ts => (ts as SqlTableSource)?.Source as SqlTable)
							.FirstOrDefault(t => t != null && QueryHelper.IsEqualTables(t, tableToUpdate));
					}
					else
					{
						// return first found table
						tableToUpdate = QueryHelper.EnumerateJoinedSources(statement.SelectQuery)
							.Select(ts => (ts as SqlTableSource)?.Source as SqlTable)
							.FirstOrDefault(t => t != null);
						tableToCompare = tableToUpdate;
					}

					if (tableToUpdate == null || tableToCompare == null)
						throw new LinqToDBException("Query can't be translated to UPDATE Statement.");

					if (ReferenceEquals(tableToUpdate, tableToCompare))
					{
						// we have to inroduce new table
						tableToUpdate = tableToUpdate.Clone();
					}

					// consider to create additional where

					var keys1 = tableToUpdate. GetKeys(true);
					var keys2 = tableToCompare.GetKeys(true);

					for (int i = 0; i < keys1.Count; i++)
					{
						statement.SelectQuery.Where
							.Expr(keys1[i]).Equal.Expr(keys2[i]);
					}

					statement.Update.Table = tableToUpdate;
				}
			}
			return statement;
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlBinaryExpression)
			{
				var be = (SqlBinaryExpression)expr;

				switch (be.Operation)
				{
					case "^": return new SqlBinaryExpression(be.SystemType, be.Expr1, "#", be.Expr2);
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence): expr;
				}
			}
			else if (expr is SqlFunction)
			{
				var func = (SqlFunction) expr;

				switch (func.Name)
				{
					case "Convert"   :
						if (func.SystemType.ToUnderlying() == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, 1);
							if (ex != null)
								return ex;
						}

						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);

					case "CharIndex" :
						return func.Parameters.Length == 2?
							new SqlExpression(func.SystemType, "Position({0} in {1})", Precedence.Primary, func.Parameters[0], func.Parameters[1]):
							Add<int>(
								new SqlExpression(func.SystemType, "Position({0} in {1})", Precedence.Primary, func.Parameters[0],
									ConvertExpression(new SqlFunction(typeof(string), "Substring",
										func.Parameters[1],
										func.Parameters[2],
										Sub<int>(ConvertExpression(new SqlFunction(typeof(int), "Length", func.Parameters[1])), func.Parameters[2])))),
								Sub(func.Parameters[2], 1));
				}
			}
			else if (expr is SqlExpression)
			{
				var e = (SqlExpression)expr;

				if (e.Expr.StartsWith("Cast(Floor(Extract(DOW"))
					return Inc(new SqlExpression(expr.SystemType, e.Expr.Replace("Extract(DOW", "Extract(Dow"), e.Parameters));

				if (e.Expr.StartsWith("Cast(Floor(Extract(Millisecond"))
					return new SqlExpression(expr.SystemType, "Cast(To_Char({0}, 'MS') as int)", e.Parameters);
			}

			return expr;
		}

	}
}
