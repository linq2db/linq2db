using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using LinqToDB.Common;
using LinqToDB.Linq.Builder;

namespace LinqToDB.SqlQuery
{
	public readonly struct CloneVisitor<TContext>
	{
		private readonly Dictionary<IQueryElement, IQueryElement> _objectTree;
		private readonly TContext?                                _context;
		private readonly Func<TContext, IQueryElement, bool>?     _doClone;
		private readonly Func<IQueryElement, bool>?               _doCloneStatic;

		internal CloneVisitor(Dictionary<IQueryElement, IQueryElement>? objectTree, TContext context, Func<TContext, IQueryElement, bool>? doClone)
		{
			_objectTree    = objectTree ?? new ();
			_context       = context;
			_doClone       = doClone;
			_doCloneStatic = null;
		}

		internal CloneVisitor(Dictionary<IQueryElement, IQueryElement>? objectTree, Func<IQueryElement, bool>? doClone)
		{
			_objectTree    = objectTree ?? new ();
			_context       = default;
			_doClone       = null;
			_doCloneStatic = doClone;
		}

		[return: NotNullIfNotNull("elements")]
		public T[]? Clone<T>(T[]? elements)
			where T : class, IQueryElement
		{
			if (elements == null)
				return null;

			if (elements.Length == 0)
				return Array<T>.Empty;

			var newArr = new T[elements.Length];

			for (var i = 0; i < elements.Length; i++)
				newArr[i] = Clone(elements[i]);

			return newArr;
		}

		private void CloneInto<T>(IList<T> target, IList<T>? source)
			where T : class, IQueryElement
		{
			if (source == null)
				return;

			foreach (var item in source)
				target!.Add(Clone(item));
		}

		private void CloneInto<T>(IList<T[]> target, IList<T[]>? source)
			where T : class, IQueryElement
		{
			if (source == null)
				return;

			foreach (var item in source)
				target!.Add(Clone(item));
		}

		private SqlSelectClause Clone(SelectQuery selectQuery, SqlSelectClause selectClause)
		{
			var newSelect = new SqlSelectClause(selectQuery)
			{
				IsDistinct = selectClause.IsDistinct,
				TakeValue  = Clone(selectClause.TakeValue),
				SkipValue  = Clone(selectClause.SkipValue),
			};

			CloneInto(newSelect.Columns, selectClause.Columns);

			return newSelect;
		}

		private SqlFromClause Clone(SelectQuery selectQuery, SqlFromClause from)
		{
			var newFrom = new SqlFromClause(selectQuery);
			CloneInto(newFrom.Tables, from.Tables);
			return newFrom;
		}

		private SqlOrderByClause Clone(SelectQuery selectQuery, SqlOrderByClause orderBy)
		{
			var newOrderBy = new SqlOrderByClause(selectQuery);
			CloneInto(newOrderBy.Items, orderBy.Items);
			return newOrderBy;
		}

		private SqlWhereClause Clone(SelectQuery selectQuery, SqlWhereClause where)
		{
			return new SqlWhereClause(selectQuery)
			{
				SearchCondition = Clone(where.SearchCondition)
			};
		}

		private SqlGroupByClause Clone(SelectQuery selectQuery, SqlGroupByClause groupBy)
		{
			var newGroupBy = new SqlGroupByClause(selectQuery)
			{
				GroupingType = groupBy.GroupingType
			};

			CloneInto(newGroupBy.Items, groupBy.Items);
			return newGroupBy;
		}

		// note on clone implementation:
		// 1. when cloning element, add it first to _objectTree before cloing it's members to avoid issues, when child elements contain reference to current element
		// 2. use _objectTree.Add() instead of _objectTree[e] = clone; to detect double clone errors
		[return: NotNullIfNotNull("element")]
		internal T? Clone<T>(T? element)
			where T: class, IQueryElement
		{
			if (element == null)
				return null;

			if (_doCloneStatic != null ? !_doCloneStatic(element) : _doClone?.Invoke(_context!, element) == false)
				return element;

			if (_objectTree.TryGetValue(element, out var clone))
				return (T)clone;

			switch (element.ElementType)
			{
				case QueryElementType.CteClause:
				{
					var cteClause = (CteClause)(IQueryElement)element;
					CteClause newCteClause;

					_objectTree.Add(element, clone = newCteClause = new CteClause(
						cteClause.ObjectType,
						cteClause.IsRecursive,
						cteClause.Name));

					newCteClause.Body   = Clone(cteClause.Body);
					newCteClause.Fields = Clone(cteClause.Fields);
					break;
				}

				case QueryElementType.SqlQuery:
				{
					var selectQuery    = (SelectQuery)(IQueryElement)element;
					var newSelectQuery = new SelectQuery(Interlocked.Increment(ref SelectQuery.SourceIDCounter))
					{
						IsParameterDependent = selectQuery.IsParameterDependent,
						DoNotRemove          = selectQuery.DoNotRemove
					};

					_objectTree.Add(element, clone = newSelectQuery);

					_objectTree.Add(selectQuery.All, newSelectQuery.All);

					if (selectQuery.ParentSelect != null)
						newSelectQuery.ParentSelect = _objectTree.TryGetValue(selectQuery.ParentSelect, out var parentClone) ? (SelectQuery)parentClone : selectQuery.ParentSelect;

					newSelectQuery.Select  = Clone(newSelectQuery, selectQuery.Select);
					newSelectQuery.From    = Clone(newSelectQuery, selectQuery.From);
					newSelectQuery.Where   = Clone(newSelectQuery, selectQuery.Where);
					newSelectQuery.GroupBy = Clone(newSelectQuery, selectQuery.GroupBy);
					newSelectQuery.Having  = Clone(newSelectQuery, selectQuery.Having);
					newSelectQuery.OrderBy = Clone(newSelectQuery, selectQuery.OrderBy);

					if (selectQuery.HasSetOperators)
						CloneInto(newSelectQuery.SetOperators, selectQuery.SetOperators);

					if (selectQuery.HasUniqueKeys)
						CloneInto(newSelectQuery.UniqueKeys, selectQuery.UniqueKeys);

					newSelectQuery.Visit((newSelectQuery, selectQuery), static (context, expr) =>
					{
						if (expr is SelectQuery sb && sb.ParentSelect == context.selectQuery)
							sb.ParentSelect = context.newSelectQuery;
					});

					break;
				}

				case QueryElementType.SetOperator:
				{
					var set = (SqlSetOperator)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlSetOperator(Clone(set.SelectQuery), set.Operation));
					break;
				}

				case QueryElementType.SqlAliasPlaceholder:
					_objectTree.Add(element, clone = new SqlAliasPlaceholder());
					break;

				case QueryElementType.SqlBinaryExpression:
				{
					var binary = (SqlBinaryExpression)(IQueryElement)element;

					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlBinaryExpression(
						binary.SystemType,
						Clone(binary.Expr1),
						binary.Operation,
						Clone(binary.Expr2),
						binary.Precedence));

					break;
				}

				case QueryElementType.Column:
				{
					var column = (SqlColumn)(IQueryElement)element;

					// TODO: children Clone called before _objectTree update (original cloning logic)
					var parent = Clone(column.Parent);
					if (!_objectTree.TryGetValue(element, out clone))
						_objectTree.Add(element, clone = new SqlColumn(
							parent,
							Clone(column.Expression),
							column.RawAlias));
					break;
				}

				case QueryElementType.Comment:
				{
					// TODO: do we really need cloning here?
					var comment = (SqlComment)(IQueryElement)element;
					_objectTree.Add(element, clone = new SqlComment(comment.Lines));
					break;
				}

				case QueryElementType.Condition:
				{
					var condition = (SqlCondition)(IQueryElement)element;

					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlCondition(condition.IsNot, Clone(condition.Predicate), condition.IsOr));
					break;
				}

				case QueryElementType.SqlCteTable:
				{
					var cteTable = (SqlCteTable)(IQueryElement)element;

					if (cteTable.Cte == null)
						throw new InvalidOperationException("Cte is null");

					// TODO: children Clone called before _objectTree update (original cloning logic)
					var table = new SqlCteTable(
							cteTable,
							Array<SqlField>.Empty,
							Clone(cteTable.Cte))
					{
						Name         = cteTable.BaseName,
						Alias        = cteTable.Alias,
						Server       = cteTable.Server,
						Database     = cteTable.Database,
						Schema       = cteTable.Schema,
						PhysicalName = cteTable.BasePhysicalName,
						ObjectType   = cteTable.ObjectType,
						SqlTableType = cteTable.SqlTableType,
					};

					table.ClearFields();

					foreach (var field in cteTable.Fields)
					{
						// TODO: use Clone(field)?
						var fc = new SqlField(field);

						_objectTree.Add(field, fc);
						table.Add(fc);
					}

					_objectTree.Add(cteTable, table);
					_objectTree.Add(cteTable.All, table.All);

					clone = table;

					break;
				}

				case QueryElementType.CreateTableStatement:
				{
					var createTable = (SqlCreateTableStatement)(IQueryElement)element;

					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlCreateTableStatement(Clone(createTable.Table)));
					break;
				}

				case QueryElementType.SqlDataType:
				{
					// TODO: cloning not needed?
					var type = (SqlDataType)(IQueryElement)element;
					_objectTree.Add(element, clone = new SqlDataType(type.Type));
					break;
				}

				case QueryElementType.DeleteStatement:
				{
					var delete = (SqlDeleteStatement)(IQueryElement)element;

					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlDeleteStatement()
					{
						Tag         = Clone(delete.Tag),
						SelectQuery = Clone(delete.SelectQuery),
						Table       = Clone(delete.Table),
						With        = Clone(delete.With),
						Output      = Clone(delete.Output)
					});

					break;
				}

				case QueryElementType.DropTableStatement:
				{
					// TODO: children Clone called before _objectTree update (original cloning logic)
					var drop = (SqlDropTableStatement)(IQueryElement)element;
					_objectTree.Add(element, clone = new SqlDropTableStatement(Clone(drop.Table))
					{
						Tag = Clone(drop.Tag)
					});
					break;
				}

				case QueryElementType.SqlExpression:
				{
					var expr = (SqlExpression)(IQueryElement)element;

					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlExpression(
						expr.SystemType,
						expr.Expr,
						expr.Precedence,
						Clone(expr.Parameters)));
					break;
				}

				case QueryElementType.SqlField:
				{
					var field = (SqlField)(IQueryElement)element;

					// TODO: that's unusual logic (from original Clone method)
					if (field.Table != null)
					{
						var table = Clone(field.Table);
						if (table == field.Table)
							return (T)(IQueryElement)field;
						return (T)_objectTree[field];
					}

					_objectTree.Add(element, clone = new SqlField(field));
					break;
				}

				case QueryElementType.SqlFunction:
				{
					var function = (SqlFunction)(IQueryElement)element;

					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlFunction(
						function.SystemType,
						function.Name,
						function.IsAggregate,
						function.Precedence,
						Clone(function.Parameters))
						{
							CanBeNull     = function.CanBeNull,
							DoNotOptimize = function.DoNotOptimize
						});
					break;
				}

				case QueryElementType.GroupingSet:
				{
					var groupingSet = (SqlGroupingSet)(IQueryElement)element;

					// TODO: children Clone called before _objectTree update (original cloning logic)
					var newSet = new SqlGroupingSet();
					CloneInto(newSet.Items, groupingSet.Items);

					_objectTree.Add(element, clone = newSet);
					break;
				}

				case QueryElementType.InsertClause:
				{
					var insert = (SqlInsertClause)(IQueryElement)element;

					// TODO: children Clone called before _objectTree update (original cloning logic)
					var newInsert = new SqlInsertClause()
					{
						WithIdentity = insert.WithIdentity,
						Into         = Clone(insert.Into)
					};
					CloneInto(newInsert.Items, insert.Items);

					_objectTree.Add(element, clone = newInsert);
					break;
				}

				case QueryElementType.InsertOrUpdateStatement:
				{
					var insertOrUpdate = (SqlInsertOrUpdateStatement)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					var newInsertOrUpdate = new SqlInsertOrUpdateStatement(Clone(insertOrUpdate.SelectQuery))
					{
						Tag     = Clone(insertOrUpdate.Tag),
						With    = Clone(insertOrUpdate.With)
					};

					if (insertOrUpdate.HasInsert)
						newInsertOrUpdate.Insert = Clone(insertOrUpdate.Insert);
					if (insertOrUpdate.HasUpdate)
						newInsertOrUpdate.Update = Clone(insertOrUpdate.Update);

					_objectTree.Add(element, clone = newInsertOrUpdate);

					break;
				}

				case QueryElementType.InsertStatement:
				{
					var insert = (SqlInsertStatement)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					var newInsert = new SqlInsertStatement(Clone(insert.SelectQuery))
					{
						Tag     = Clone(insert.Tag),
						With    = Clone(insert.With)
					};

					if (insert.HasInsert)
						newInsert.Insert = Clone(insert.Insert);

					_objectTree.Add(element, clone = newInsert);

					break;
				}

				case QueryElementType.JoinedTable:
				{
					var table = (SqlJoinedTable)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlJoinedTable(
						table.JoinType,
						Clone(table.Table),
						table.IsWeak,
						Clone(table.Condition)));
					break;
				}

				case QueryElementType.SqlObjectExpression:
				{
					var expr = (SqlObjectExpression)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					// TODO: SqlInfo in AST?
					var newInfoParameters = expr.InfoParameters.Length > 0 ? new SqlInfo[expr.InfoParameters.Length] : Array<SqlInfo>.Empty;
					for (var i = 0; i < newInfoParameters.Length; i++)
						newInfoParameters[i] = expr.InfoParameters[i].WithSql(Clone(expr.InfoParameters[i].Sql));

					_objectTree.Add(element, clone = new SqlObjectExpression(expr.MappingSchema, newInfoParameters));
					break;
				}

				case QueryElementType.OrderByItem:
				{
					var item = (SqlOrderByItem)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlOrderByItem(Clone(item.Expression), item.IsDescending));
					break;
				}

				case QueryElementType.OutputClause:
				{
					var output = (SqlOutputClause)(IQueryElement)element;
					SqlOutputClause newOutput;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					// TODO: tables not cloned (original logic)
					clone = newOutput = new SqlOutputClause()
					{
						SourceTable   = output.SourceTable,
						DeletedTable  = output.DeletedTable,
						InsertedTable = output.InsertedTable,
						OutputTable   = output.OutputTable
					};

					if (output.HasOutputItems)
						CloneInto(newOutput.OutputItems, output.OutputItems);

					_objectTree.Add(element, newOutput);
					break;
				}

				case QueryElementType.SqlParameter:
				{
					// we do not allow parameters cloning
					_objectTree.Add(element, clone = element);
					break;
				}

				case QueryElementType.ExprPredicate:
				{
					var expr = (SqlPredicate.Expr)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlPredicate.Expr(Clone(expr.Expr1), expr.Precedence));
					break;
				}

				case QueryElementType.NotExprPredicate:
				{
					var expr = (SqlPredicate.NotExpr)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlPredicate.NotExpr(Clone(expr.Expr1), expr.IsNot, expr.Precedence));
					break;
				}

				case QueryElementType.ExprExprPredicate:
				{
					var expr = (SqlPredicate.ExprExpr)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlPredicate.ExprExpr(Clone(expr.Expr1), expr.Operator, Clone(expr.Expr2), expr.WithNull));
					break;
				}

				case QueryElementType.LikePredicate:
				{
					var expr = (SqlPredicate.Like)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlPredicate.Like(Clone(expr.Expr1), expr.IsNot, Clone(expr.Expr2), expr.Escape));
					break;
				}

				case QueryElementType.SearchStringPredicate:
				{
					var expr = (SqlPredicate.SearchString)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlPredicate.SearchString(Clone(expr.Expr1), expr.IsNot, Clone(expr.Expr2), expr.Kind, expr.IgnoreCase));
					break;
				}

				case QueryElementType.BetweenPredicate:
				{
					var expr = (SqlPredicate.Between)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlPredicate.Between(Clone(expr.Expr1), expr.IsNot, Clone(expr.Expr2), Clone(expr.Expr3)));
					break;
				}

				case QueryElementType.IsTruePredicate:
				{
					var expr = (SqlPredicate.IsTrue)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlPredicate.IsTrue(Clone(expr.Expr1), expr.TrueValue, expr.FalseValue, expr.WithNull, expr.IsNot));
					break;
				}

				case QueryElementType.IsNullPredicate:
				{
					var expr = (SqlPredicate.IsNull)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlPredicate.IsNull(Clone(expr.Expr1), expr.IsNot));
					break;
				}

				case QueryElementType.InSubQueryPredicate:
				{
					var expr = (SqlPredicate.InSubQuery)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlPredicate.InSubQuery(Clone(expr.Expr1), expr.IsNot, Clone(expr.SubQuery)));
					break;
				}

				case QueryElementType.InListPredicate:
				{
					var expr = (SqlPredicate.InList)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					var newExpr = new SqlPredicate.InList(Clone(expr.Expr1), expr.WithNull, expr.IsNot);
					CloneInto(newExpr.Values, expr.Values);
					_objectTree.Add(element, clone = newExpr);
					break;
				}

				case QueryElementType.FuncLikePredicate:
				{
					var expr = (SqlPredicate.FuncLike)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlPredicate.FuncLike(Clone(expr.Function)));
					break;
				}

				case QueryElementType.SearchCondition:
				{
					var search = (SqlSearchCondition)(IQueryElement)element;

					var sc = new SqlSearchCondition();

					_objectTree.Add(element, clone = sc);

					CloneInto(sc.Conditions, search.Conditions);
					break;
				}

				case QueryElementType.SelectStatement:
				{
					var select = (SqlSelectStatement)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlSelectStatement()
					{
						Tag         = Clone(select.Tag),
						SelectQuery = Clone(select.SelectQuery),
						With        = Clone(select.With),
					});
					break;
				}

				case QueryElementType.SetExpression:
				{
					var set = (SqlSetExpression)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlSetExpression(Clone(set.Column), Clone(set.Expression)));
					break;
				}

				case QueryElementType.SqlTable:
				{
					var table = (SqlTable)(IQueryElement)element;

					var newTable = new SqlTable()
					{
						Name               = table.Name,
						Alias              = table.Alias,
						Server             = table.Server,
						Database           = table.Database,
						Schema             = table.Schema,
						PhysicalName       = table.PhysicalName,
						ObjectType         = table.ObjectType,
						SqlTableType       = table.SqlTableType,
						SequenceAttributes = table.SequenceAttributes,
					};

					newTable.ClearFields();

					foreach (var field in table.Fields)
					{
						var fc = new SqlField(field);

						_objectTree.Add(field, fc);
						newTable.Add(fc);
					}

					newTable.TableArguments = Clone(table.TableArguments);

					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element  , clone = newTable);
					_objectTree.Add(table.All, newTable.All);
					break;
				}

				case QueryElementType.TableSource:
				{
					var ts = (SqlTableSource)(IQueryElement)element;

					// TODO: Source Clone called before _objectTree update (original cloning logic)
					var newTs = new SqlTableSource(Clone(ts.Source), ts._alias);

					_objectTree.Add(element, clone = newTs);

					CloneInto(newTs.Joins, ts.Joins);

					if (ts.HasUniqueKeys)
						CloneInto(newTs.UniqueKeys, ts.UniqueKeys);
					break;
				}

				case QueryElementType.TruncateTableStatement:
				{
					var trunc = (SqlTruncateTableStatement)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					_objectTree.Add(element, clone = new SqlTruncateTableStatement()
					{
						Tag   = Clone(trunc.Tag),
						Table = Clone(trunc.Table),
					});
					break;
				}

				case QueryElementType.UpdateClause:
				{
					var update = (SqlUpdateClause)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					var newUpdate = new SqlUpdateClause()
					{
						Table = Clone(update.Table)
					};

					CloneInto(newUpdate.Items, update.Items);
					CloneInto(newUpdate.Keys, update.Keys);

					_objectTree.Add(element, clone = newUpdate);
					break;
				}

				case QueryElementType.UpdateStatement:
				{
					var update = (SqlUpdateStatement)(IQueryElement)element;
					// TODO: children Clone called before _objectTree update (original cloning logic)
					var newUpdate = new SqlUpdateStatement(Clone(update.SelectQuery))
					{
						Tag     = Clone(update.Tag),
						With    = Clone(update.With)
					};

					if (update.HasUpdate)
						newUpdate.Update = Clone(update.Update);

					_objectTree.Add(element, clone = newUpdate);
					break;
				}

				case QueryElementType.SqlValue:
				{
					// TODO: do we really need cloning here?
					var value = (SqlValue)(IQueryElement)element;
					_objectTree.Add(element, clone = new SqlValue(value.ValueType, value.Value));
					break;
				}

				case QueryElementType.WithClause:
				{
					var with = (SqlWithClause)(IQueryElement)element;

					// TODO: children Clone called before _objectTree update (original cloning logic)
					var newWith = new SqlWithClause();
					CloneInto(newWith.Clauses, with.Clauses);
					_objectTree.Add(element, clone = newWith);
					break;
				}

				// types below had explicit NOT IMPLEMENTED implementation
				//case QueryElementType.ConditionalInsertClause:
				//case QueryElementType.MergeOperationClause;
				//case QueryElementType.MergeStatement:
				//case QueryElementType.MultiInsertStatement:
				//case QueryElementType.MergeSourceTable:
				//case QueryElementType.SqlValuesTable:
				default:
					throw new NotImplementedException($"Unsupported query element type: {element.GetType()} ({element.ElementType})");
			}

			return (T)clone;
		}
	}
}
