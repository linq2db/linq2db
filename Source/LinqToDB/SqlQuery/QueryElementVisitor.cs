using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LinqToDB.SqlQuery
{
	public enum VisitMode
	{
		ReadOnly,
		Modify,
		Transform
	}

	public abstract class QueryElementVisitor
	{
		protected QueryElementVisitor(VisitMode visitMode)
		{
			VisitMode = visitMode;
		}

		public VisitMode VisitMode { get; }

		public virtual VisitMode GetVisitMode(IQueryElement element) => VisitMode;

		// usually triggered by cloning
		public virtual bool ShouldReplace(IQueryElement element) => false;

		[return: NotNullIfNotNull(nameof(element))]
		public virtual IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null) 
				return element;

			switch (element.ElementType)
			{
				case QueryElementType.SqlField:
					return VisitSqlFieldReference((SqlField)element);
				case QueryElementType.SqlFunction:
					return VisitSqlFunction((SqlFunction)element);
				case QueryElementType.SqlParameter:
					return VisitSqlParameter((SqlParameter)element);
				case QueryElementType.SqlExpression:
					return VisitSqlExpression((SqlExpression)element);
				case QueryElementType.SqlNullabilityExpression:
					return VisitSqlNullabilityExpression((SqlNullabilityExpression)element);
				case QueryElementType.SqlAnchor:
					return VisitSqlAnchor((SqlAnchor)element);
				case QueryElementType.SqlObjectExpression:
					return VisitSqlObjectExpression((SqlObjectExpression)element);
				case QueryElementType.SqlBinaryExpression:
					return VisitSqlBinaryExpression((SqlBinaryExpression)element);
				case QueryElementType.SqlValue:
					return VisitSqlValue((SqlValue)element);
				case QueryElementType.SqlDataType:
					return VisitSqlDataType((SqlDataType)element);
				case QueryElementType.SqlTable:
					return VisitSqlTable((SqlTable)element);
				case QueryElementType.SqlAliasPlaceholder:
					return VisitSqlAliasPlaceholder((SqlAliasPlaceholder)element);
				case QueryElementType.SqlRow:
					return VisitSqlRow((SqlRow)element);
				case QueryElementType.ExprPredicate:
					return VisitExprPredicate((SqlPredicate.Expr)element);
				case QueryElementType.NotExprPredicate:
					return VisitNotExprPredicate((SqlPredicate.NotExpr)element);
				case QueryElementType.ExprExprPredicate:
					return VisitExprExprPredicate((SqlPredicate.ExprExpr)element);
				case QueryElementType.LikePredicate:
					return VisitLikePredicate((SqlPredicate.Like)element);
				case QueryElementType.SearchStringPredicate:
					return VisitSearchStringPredicate((SqlPredicate.SearchString)element);
				case QueryElementType.BetweenPredicate:
					return VisitBetweenPredicate((SqlPredicate.Between)element);
				case QueryElementType.IsNullPredicate:
					return VisitIsNullPredicate((SqlPredicate.IsNull)element);
				case QueryElementType.IsDistinctPredicate:
					return VisitIsDistinctPredicate((SqlPredicate.IsDistinct)element);
				case QueryElementType.IsTruePredicate:
					return VisitIsTruePredicate((SqlPredicate.IsTrue)element);
				case QueryElementType.InSubQueryPredicate:
					return VisitInSubQueryPredicate((SqlPredicate.InSubQuery)element);
				case QueryElementType.InListPredicate:
					return VisitInListPredicate((SqlPredicate.InList)element);
				case QueryElementType.FuncLikePredicate:
					return VisitFuncLikePredicate((SqlPredicate.FuncLike)element);
				case QueryElementType.SqlQuery:
					return VisitSqlQuery((SelectQuery)element);
				case QueryElementType.Column:
					return VisitSqlColumnReference((SqlColumn)element);
				case QueryElementType.SearchCondition:
					return VisitSqlSearchCondition((SqlSearchCondition)element);
				case QueryElementType.Condition:
					return VisitSqlCondition((SqlCondition)element);
				case QueryElementType.TableSource:
					return VisitSqlTableSource((SqlTableSource)element);
				case QueryElementType.JoinedTable:
					return VisitSqlJoinedTable((SqlJoinedTable)element);
				case QueryElementType.SelectClause:
					return VisitSqlSelectClause((SqlSelectClause)element);
				case QueryElementType.InsertClause:
					return VisitSqlInsertClause((SqlInsertClause)element);
				case QueryElementType.UpdateClause:
					return VisitSqlUpdateClause((SqlUpdateClause)element);
				case QueryElementType.SetExpression:
					return VisitSqlSetExpression((SqlSetExpression)element);
				case QueryElementType.FromClause:
					return VisitSqlFromClause((SqlFromClause)element);
				case QueryElementType.WhereClause:
					return VisitSqlWhereClause((SqlWhereClause)element);
				case QueryElementType.GroupByClause:
					return VisitSqlGroupByClause((SqlGroupByClause)element);
				case QueryElementType.OrderByClause:
					return VisitSqlOrderByClause((SqlOrderByClause)element);
				case QueryElementType.OrderByItem:
					return VisitSqlOrderByItem((SqlOrderByItem)element);
				case QueryElementType.SetOperator:
					return VisitSqlSetOperator((SqlSetOperator)element);
				case QueryElementType.WithClause:
					return VisitSqlWithClause((SqlWithClause)element);
				case QueryElementType.CteClause:
					return VisitCteClause((CteClause)element);
				case QueryElementType.SqlCteTable:
					return VisitSqlCteTable((SqlCteTable)element);
				case QueryElementType.SqlRawSqlTable:
					return VisitSqlRawSqlTable((SqlRawSqlTable)element);
				case QueryElementType.SqlValuesTable:
					return VisitSqlValuesTable((SqlValuesTable)element);
				case QueryElementType.OutputClause:
					return VisitSqlOutputClause((SqlOutputClause)element);
				case QueryElementType.SelectStatement:
					return VisitSqlSelectStatement((SqlSelectStatement)element);
				case QueryElementType.InsertStatement:
					return VisitSqlInsertStatement((SqlInsertStatement)element);
				case QueryElementType.InsertOrUpdateStatement:
					return VisitSqlInsertOrUpdateStatement((SqlInsertOrUpdateStatement)element);
				case QueryElementType.UpdateStatement:
					return VisitSqlUpdateStatement((SqlUpdateStatement)element);
				case QueryElementType.DeleteStatement:
					return VisitSqlDeleteStatement((SqlDeleteStatement)element);
				case QueryElementType.MergeStatement:
					return VisitSqlMergeStatement((SqlMergeStatement)element);
				case QueryElementType.MultiInsertStatement:
					return VisitSqlMultiInsertStatement((SqlMultiInsertStatement)element);
				case QueryElementType.ConditionalInsertClause:
					return VisitSqlConditionalInsertClause((SqlConditionalInsertClause)element);
				case QueryElementType.CreateTableStatement:
					return VisitSqlCreateTableStatement((SqlCreateTableStatement)element);
				case QueryElementType.DropTableStatement:
					return VisitSqlDropTableStatement((SqlDropTableStatement)element);
				case QueryElementType.TruncateTableStatement:
					return VisitSqlTruncateTableStatement((SqlTruncateTableStatement)element);
				case QueryElementType.SqlTableLikeSource:
					return VisitSqlTableLikeSource((SqlTableLikeSource)element);
				case QueryElementType.MergeOperationClause:
					return VisitSqlMergeOperationClause((SqlMergeOperationClause)element);
				case QueryElementType.GroupingSet:
					return VisitSqlGroupingSet((SqlGroupingSet)element);
				case QueryElementType.Comment:
					return VisitSqlComment((SqlComment)element);
				case QueryElementType.SqlID:
					throw new NotImplementedException();
				case QueryElementType.SqlExtension:
					return VisitSqlExtension((IQueryExtension)element);
				default:
					throw new InvalidOperationException();
			}
		}

		public IQueryElement VisitSqlExtension(IQueryExtension element)
		{
			return element.Accept(this);
		}

		public virtual IQueryElement VisitSqlComment(SqlComment element) => element;

		public virtual IQueryElement VisitSqlGroupingSet(SqlGroupingSet element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Items, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					VisitElements(element.Items, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var items = VisitElements(element.Items, VisitMode.Transform);

					if (ShouldReplace(element) || items != null && !ReferenceEquals(element.Items, items))
					{
						return NotifyReplaced(new SqlGroupingSet(items ?? element.Items), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlMergeOperationClause(SqlMergeOperationClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Where);
					Visit(element.WhereDelete);

					VisitElements(element.Items, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					element.Where       = (SqlSearchCondition?)Visit(element.Where);
					element.WhereDelete = (SqlSearchCondition?)Visit(element.WhereDelete);

					VisitElements(element.Items, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var where       = (SqlSearchCondition?)Visit(element.Where);
					var whereDelete = (SqlSearchCondition?)Visit(element.WhereDelete);
					var items       = VisitElements(element.Items, VisitMode.Transform);

					if (ShouldReplace(element)                             || 
					    !ReferenceEquals(element.Where, where)             ||
					    !ReferenceEquals(element.WhereDelete, whereDelete) ||
					    items != null && !ReferenceEquals(element.Items, items))
					{
						return NotifyReplaced(new SqlMergeOperationClause(
								element.OperationType,
								where,
								whereDelete,
								items ?? element.Items),
							element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlTableLikeSource(SqlTableLikeSource element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.SourceEnumerable);
					Visit(element.SourceQuery);

					break;
				}
				case VisitMode.Modify:
				{
					element.SourceEnumerable = (SqlValuesTable?)Visit(element.SourceEnumerable);
					element.SourceQuery      = (SelectQuery?)Visit(element.SourceQuery);

					break;
				}
				case VisitMode.Transform:
				{
					var sourceEnumerable = (SqlValuesTable?)Visit(element.SourceEnumerable);
					var sourceQuery      = (SelectQuery?)Visit(element.SourceQuery);

					if (ShouldReplace(element)                                       || 
					    !ReferenceEquals(element.SourceEnumerable, sourceEnumerable) ||
					    !ReferenceEquals(element.SourceQuery, sourceQuery))
					{
						var newFields = new SqlField[element.SourceFields.Count];
						for (var i = 0; i < element.SourceFields.Count; i++)
						{
							var oldField = element.SourceFields[i];
							var newField = newFields[i] = new SqlField(oldField);
							NotifyReplaced(newField, oldField);
						}

						NotifyReplaced(new SqlTableLikeSource(
							element.SourceID,
							sourceEnumerable ?? throw new InvalidOperationException(),
							sourceQuery      ?? throw new InvalidOperationException(),
							newFields), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlTruncateTableStatement(SqlTruncateTableStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.Table);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag   = (SqlComment?)Visit(element.Tag);
					element.Table = (SqlTable?)Visit(element.Table);

					break;
				}
				case VisitMode.Transform:
				{
					var tag   = (SqlComment?)Visit(element.Tag);
					var table = (SqlTable?)Visit(element.Table);

					if (ShouldReplace(element)             || 
					    !ReferenceEquals(element.Tag, tag) ||
					    !ReferenceEquals(element.Table, table))
					{
						return NotifyReplaced(
							new SqlTruncateTableStatement
							{
								Tag = tag, Table = table, 
								ResetIdentity = element.ResetIdentity
							}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlDropTableStatement(SqlDropTableStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.Table);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag   = (SqlComment?)Visit(element.Tag);
					var table = (SqlTable)Visit(element.Table);

					element.Modify(table);

					break;
				}
				case VisitMode.Transform:
				{
					var tag   = (SqlComment?)Visit(element.Tag);
					var table = (SqlTable)Visit(element.Table);

					if (ShouldReplace(element)             || 
					    !ReferenceEquals(element.Tag, tag) ||
					    !ReferenceEquals(element.Table, table))
					{
						return NotifyReplaced(new SqlCreateTableStatement(table) { Tag = tag }, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}
		
			return element;
		}

		public virtual IQueryElement VisitSqlCreateTableStatement(SqlCreateTableStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.Table);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag = (SqlComment?)Visit(element.Tag);
					var table = (SqlTable)Visit(element.Table);

					element.Modify(table);

					break;
				}
				case VisitMode.Transform:
				{
					var tag   = (SqlComment?)Visit(element.Tag);
					var table = (SqlTable)Visit(element.Table);

					if (ShouldReplace(element)             ||
					    !ReferenceEquals(element.Tag, tag) ||
					    !ReferenceEquals(element.Table, table))
					{
						return NotifyReplaced(new SqlCreateTableStatement(table) { Tag = tag }, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}
		
			return element;
		}

		public virtual IQueryElement VisitSqlConditionalInsertClause(SqlConditionalInsertClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Insert);
					Visit(element.When);

					break;
				}
				case VisitMode.Modify:
				{
					var insert = (SqlInsertClause)Visit(element.Insert);
					var when   = (SqlSearchCondition?)Visit(element.When);

					element.Modify(insert, when);

					break;
				}
				case VisitMode.Transform:
				{
					var insert = (SqlInsertClause)Visit(element.Insert);
					var when   = (SqlSearchCondition?)Visit(element.When);

					if (ShouldReplace(element)                   ||
					    !ReferenceEquals(element.Insert, insert) ||
					    !ReferenceEquals(element.When, when))
					{
						return NotifyReplaced(new SqlConditionalInsertClause(insert, when), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}
		
			return element;
		}

		public virtual IQueryElement VisitSqlMultiInsertStatement(SqlMultiInsertStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Source);
					VisitElements(element.Inserts, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					var source = (SqlTableLikeSource)Visit(element.Source);
					VisitElements(element.Inserts, VisitMode.Modify);

					element.Modify(source, element.Inserts);

					break;
				}
				case VisitMode.Transform:
				{
					var source  = (SqlTableLikeSource)Visit(element.Source);
					var inserts = VisitElements(element.Inserts, VisitMode.Transform);

					if (!ReferenceEquals(element.Source, source) ||
					    inserts != null && !ReferenceEquals(element.Inserts, inserts))
					{
						return NotifyReplaced(new SqlMultiInsertStatement(
								element.InsertType,
								source,
								inserts ?? element.Inserts),
							element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}
		
			return element;
		}

		public virtual IQueryElement VisitSqlMergeStatement(SqlMergeStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.With);
					Visit(element.Target);
					Visit(element.Source);
					Visit(element.On);
					Visit(element.Output);
					VisitElements(element.Operations, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag        = (SqlComment?)Visit(element.Tag);
					element.With       = (SqlWithClause?)Visit(element.With);

					var target     = (SqlTableSource)Visit(element.Target);
					var source     = (SqlTableLikeSource)Visit(element.Source);
					var on         = (SqlSearchCondition)Visit(element.On);
					var output     = (SqlOutputClause?)Visit(element.Output);

					VisitElements(element.Operations, VisitMode.Modify);

					element.Modify(target, source, on, element.Operations, output);

					break;
				}
				case VisitMode.Transform:
				{
					var tag        = (SqlComment?)Visit(element.Tag);
					var with       = (SqlWithClause?)Visit(element.With);

					var target     = (SqlTableSource)Visit(element.Target);
					var source     = (SqlTableLikeSource)Visit(element.Source);
					var on         = (SqlSearchCondition)Visit(element.On);
					var output     = (SqlOutputClause?)Visit(element.Output);
					var operations = VisitElements(element.Operations, VisitMode.Transform);

					if (ShouldReplace(element)                   || 
					    !ReferenceEquals(element.Tag, tag)       ||
					    !ReferenceEquals(element.With, with)     ||
					    !ReferenceEquals(element.Target, target) ||
					    !ReferenceEquals(element.Source, source) ||
					    !ReferenceEquals(element.On, on)         ||
					    !ReferenceEquals(element.Output, output) ||
					    operations != null && !ReferenceEquals(element.Operations, operations)
					   )
					{
						return NotifyReplaced(
							new SqlMergeStatement(
								with,
								element.Hint,
								target,
								source,
								on,
								operations ?? element.Operations) { Tag = tag, Output = output },
							element);
						//CorrectQueryHierarchy(((SqlSelectStatement)newElement).SelectQuery);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}
		
			return element;
		}

		public virtual IQueryElement VisitSqlDeleteStatement(SqlDeleteStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.With);
					Visit(element.SelectQuery);
					Visit(element.Table);
					Visit(element.Top);
					Visit(element.Output);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag         = (SqlComment?)Visit(element.Tag);
					element.With        = (SqlWithClause?)Visit(element.With);
					element.SelectQuery = (SelectQuery?)Visit(element.SelectQuery);
					element.Table       = (SqlTable?)Visit(element.Table);
					element.Top         = (ISqlExpression?)Visit(element.Table);
					element.Output      = (SqlOutputClause?)Visit(element.Output);

					break;
				}
				case VisitMode.Transform:
				{
					var tag         = (SqlComment?)Visit(element.Tag);
					var with        = (SqlWithClause?)Visit(element.With);
					var selectQuery = (SelectQuery?)Visit(element.SelectQuery);
					var table       = (SqlTable?)Visit(element.Table);
					var top         = (ISqlExpression?)Visit(element.Top);
					var output      = (SqlOutputClause?)Visit(element.Output);

					if (ShouldReplace(element)                             || 
					    !ReferenceEquals(element.SelectQuery, selectQuery) ||
					    !ReferenceEquals(element.Tag, tag)                 ||
					    !ReferenceEquals(element.With, with)               ||
					    !ReferenceEquals(element.Table, table)             ||
					    !ReferenceEquals(element.Top, top)                 ||
					    !ReferenceEquals(element.Output, output)
					   )
					{
						return NotifyReplaced(
							new SqlDeleteStatement(selectQuery ?? element.SelectQuery)
							{
								Tag         = tag,
								With        = with,
								SelectQuery = selectQuery,
								Table       = table,
								Top         = top,
								Output      = output
							}, element);
						//CorrectQueryHierarchy(((SqlSelectStatement)newElement).SelectQuery);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}
		
			return element;
		}

		public virtual IQueryElement VisitSqlUpdateStatement(SqlUpdateStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.With);
					Visit(element.SelectQuery);
					Visit(element.Update);
					Visit(element.Output);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag         = (SqlComment?)Visit(element.Tag);
					element.With        = (SqlWithClause?)Visit(element.With);
					element.SelectQuery = (SelectQuery?)Visit(element.SelectQuery);
					element.Update      = (SqlUpdateClause)Visit(element.Update);
					element.Output      = (SqlOutputClause?)Visit(element.Output);

					break;
				}
				case VisitMode.Transform:
				{
					var tag         = (SqlComment?)Visit(element.Tag);
					var with        = (SqlWithClause?)Visit(element.With);
					var selectQuery = (SelectQuery?)Visit(element.SelectQuery);
					var update      = (SqlUpdateClause)Visit(element.Update);
					var output      = (SqlOutputClause?)Visit(element.Output);

					if (ShouldReplace(element)                             ||
					    !ReferenceEquals(element.SelectQuery, selectQuery) ||
					    !ReferenceEquals(element.Tag, tag)                 ||
					    !ReferenceEquals(element.With, with)               ||
					    !ReferenceEquals(element.Update, update)           ||
					    !ReferenceEquals(element.Output, output)
					   )
					{
						return NotifyReplaced(
							new SqlUpdateStatement(selectQuery ?? element.SelectQuery)
							{
								Tag         = tag,
								With        = with,
								SelectQuery = selectQuery,
								Update      = update,
								Output      = output
							}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlInsertOrUpdateStatement(SqlInsertOrUpdateStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.With);
					Visit(element.SelectQuery);
					Visit(element.Insert);
					Visit(element.Update);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag         = (SqlComment?)Visit(element.Tag);
					element.With        = (SqlWithClause?)Visit(element.With);
					element.SelectQuery = (SelectQuery?)Visit(element.SelectQuery);
					element.Insert      = (SqlInsertClause)Visit(element.Insert);
					element.Update      = (SqlUpdateClause)Visit(element.Update);

					break;
				}
				case VisitMode.Transform:
				{
					var tag         = (SqlComment?)Visit(element.Tag);
					var with        = (SqlWithClause?)Visit(element.With);
					var selectQuery = (SelectQuery?)Visit(element.SelectQuery);
					var insert      = (SqlInsertClause)Visit(element.Insert);
					var update      = (SqlUpdateClause)Visit(element.Update);

					if (ShouldReplace(element)                             || 
					    !ReferenceEquals(element.SelectQuery, selectQuery) ||
					    !ReferenceEquals(element.Tag, tag)                 ||
					    !ReferenceEquals(element.With, with)               ||
					    !ReferenceEquals(element.Insert, insert)           ||
					    !ReferenceEquals(element.Update, update)
					   )
					{
						return NotifyReplaced(new SqlInsertOrUpdateStatement(selectQuery ?? element.SelectQuery)
						{
							Tag         = tag,
							With        = with,
							SelectQuery = selectQuery,
							Insert      = insert,
							Update      = update
						}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement NotifyReplaced(IQueryElement newElement, IQueryElement oldElement)
		{
			return newElement;
		}

		public virtual IQueryElement VisitSqlFieldReference(SqlField element) => element;

		public virtual IQueryElement VisitSqlInsertStatement(SqlInsertStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.With);
					Visit(element.SelectQuery);
					Visit(element.Insert);
					Visit(element.Output);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag         = (SqlComment?)Visit(element.Tag);
					element.With        = (SqlWithClause?)Visit(element.With);
					element.SelectQuery = (SelectQuery?)Visit(element.SelectQuery);
					element.Insert      = (SqlInsertClause)Visit(element.Insert);
					element.Output      = (SqlOutputClause?)Visit(element.Output);

					break;
				}
				case VisitMode.Transform:
				{
					var tag         = (SqlComment?)Visit(element.Tag);
					var with        = (SqlWithClause?)Visit(element.With);
					var selectQuery = (SelectQuery?)Visit(element.SelectQuery);
					var insert      = (SqlInsertClause)Visit(element.Insert);
					var output      = (SqlOutputClause?)Visit(element.Output);

					if (ShouldReplace(element)                             || 
					    !ReferenceEquals(element.SelectQuery, selectQuery) ||
					    !ReferenceEquals(element.Tag, tag)                 ||
					    !ReferenceEquals(element.With, with)               ||
					    !ReferenceEquals(element.Insert, insert)           ||
					    !ReferenceEquals(element.Output, output)
					    )
					{
						return NotifyReplaced(new SqlInsertStatement(selectQuery ?? element.SelectQuery)
						{
							Tag         = tag,
							With        = with,
							SelectQuery = selectQuery,
							Insert      = insert,
							Output      = output
						}, element);
						//CorrectQueryHierarchy(((SqlSelectStatement)newElement).SelectQuery);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlSelectStatement(SqlSelectStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.With);
					Visit(element.SelectQuery);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag         = (SqlComment?)Visit(element.Tag);
					element.With        = (SqlWithClause?)Visit(element.With);
					element.SelectQuery = (SelectQuery?)Visit(element.SelectQuery);

					break;
				}
				case VisitMode.Transform:
				{
					var tag         = (SqlComment?)Visit(element.Tag);
					var with        = (SqlWithClause?)Visit(element.With);
					var selectQuery = (SelectQuery?)Visit(element.SelectQuery);

					if (ShouldReplace(element)                             || 
					    !ReferenceEquals(element.SelectQuery, selectQuery) ||
					    !ReferenceEquals(element.Tag, tag)                 ||
					    !ReferenceEquals(element.With, with))
					{
						return NotifyReplaced(new SqlSelectStatement(selectQuery ?? element.SelectQuery)
						{
							Tag  = tag  ?? element.Tag,
							With = with ?? element.With
						}, element);
						//CorrectQueryHierarchy(((SqlSelectStatement)newElement).SelectQuery);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlOutputClause(SqlOutputClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.InsertedTable);
					Visit(element.DeletedTable);
					Visit(element.OutputTable);

					if (element.OutputColumns != null)
					{
						VisitElements(element.OutputColumns, VisitMode.ReadOnly);
					}

					if (element.HasOutputItems)
					{
						VisitElementsReadOnly(element.OutputItems);
					}

					break;
				}
				case VisitMode.Modify:
				{
					var insertedTable = (SqlTable?)Visit(element.InsertedTable);
					var deletedTable  = (SqlTable?)Visit(element.DeletedTable);
					var outputTable   = (SqlTable?)Visit(element.OutputTable);
					var outputColumns = element.OutputColumns != null
						? VisitElements(element.OutputColumns, VisitMode.Modify)
						: null;

					var outputItems = element.HasOutputItems ? VisitElements(element.OutputItems, VisitMode.Transform) : null;

					element.Modify(insertedTable, deletedTable, outputTable, outputColumns, outputItems);

					break;
				}
				case VisitMode.Transform:
				{
					var insertedTable = (SqlTable?)Visit(element.InsertedTable);
					var deletedTable  = (SqlTable?)Visit(element.DeletedTable);
					var outputTable   = (SqlTable?)Visit(element.OutputTable);
					var outputColumns = element.OutputColumns != null
						? VisitElements(element.OutputColumns, VisitMode.Transform)
						: null;

					List<SqlSetExpression>? outputItems = null;

					if (element.HasOutputItems)
						outputItems = VisitElements(element.OutputItems, VisitMode.Transform);

					if (ShouldReplace(element)                                                            ||
					    !ReferenceEquals(element.InsertedTable, insertedTable)                            ||
					    !ReferenceEquals(element.DeletedTable, deletedTable)                              ||
					    !ReferenceEquals(element.OutputTable, outputTable)                                ||
					    (outputColumns != null && !ReferenceEquals(element.OutputColumns, outputColumns)) ||
					    (element.HasOutputItems && outputItems != null && !ReferenceEquals(element.OutputItems, outputItems))
					   )
					{
						var newElement = NotifyReplaced(new SqlOutputClause
						{
							InsertedTable = insertedTable,
							DeletedTable  = deletedTable,
							OutputTable   = outputTable,
							OutputColumns = outputColumns,
							OutputItems   = outputItems ?? new List<SqlSetExpression>()
						}, element);

						return newElement;
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlValuesTable(SqlValuesTable element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					if (element.Rows != null)
					{
						VisitListOfArrays(element.Rows, VisitMode.ReadOnly);
					}

					break;
				}
				case VisitMode.Modify:
				{
					if (element.Rows != null)
					{
						var rows = VisitListOfArrays(element.Rows, VisitMode.Modify);
						element.Modify(rows);
					}

					break;
				}
				case VisitMode.Transform:
				{
					if (element.Rows != null)
					{
						var newRows = VisitListOfArrays(element.Rows, VisitMode.Transform);
						if (ShouldReplace(element) || newRows != null)
						{
							var prevFields = element.Fields;
							var newFields  = new SqlField[prevFields.Count];

							for (var i = 0; i < prevFields.Count; i++)
							{
								var field = prevFields[i];

								var newField = new SqlField(field);
								newFields[i] = newField;

								NotifyReplaced(newField, field);
							}

							return NotifyReplaced(new SqlValuesTable(element.Source!, element.ValueBuilders!,
								newFields, newRows ?? element.Rows), element);
						}
					}

					break;

				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlRawSqlTable(SqlRawSqlTable element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Parameters, VisitMode.ReadOnly);
					break;
				}	
				case VisitMode.Modify:
				{
					var parameters = VisitElements(element.Parameters, VisitMode.Modify);
					if (parameters != null)
						element.Modify(parameters);

					break;
				}
				case VisitMode.Transform:
				{
					var parameters = VisitElements(element.Parameters, VisitMode.Transform);

					if (ShouldReplace(element) || parameters != null && !ReferenceEquals(element.Parameters, parameters))
					{
						return NotifyReplaced(new SqlRawSqlTable(element, parameters ?? element.Parameters), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlCteTable(SqlCteTable element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					break;
				}
				case VisitMode.Modify:
				{
					break;
				}
				case VisitMode.Transform:
				{
					var clause = (CteClause?)Visit(element.Cte);

					if (ShouldReplace(element) || !ReferenceEquals(element.Cte, clause))
					{
						var newFields = element.Fields.Select(f => new SqlField(f));
						var newTable  = new SqlCteTable(element, newFields, element.Cte!);

						for (var index = 0; index < newTable.Fields.Count; index++)
						{
							NotifyReplaced(newTable.Fields[index], element.Fields[index]);
						}

						return NotifyReplaced(newTable, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlWithClause(SqlWithClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Clauses, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					var clauses = VisitElements(element.Clauses, VisitMode.Modify);
					if (clauses != null)
					{
						element.Clauses = clauses;
					}

					break;
				}
				case VisitMode.Transform:
				{
					var clauses = VisitElements(element.Clauses, VisitMode.Transform);
					if (ShouldReplace(element) || clauses != null && !ReferenceEquals(element.Clauses, clauses))
					{
						return NotifyReplaced(new SqlWithClause {Clauses = clauses ?? element.Clauses}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitCteClause(CteClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElementsReadOnly(element.Fields);
					Visit(element.Body);
					break;
				}

				case VisitMode.Modify:
				{
					element.Body = (SelectQuery?)Visit(element.Body);
					break;
				}

				case VisitMode.Transform:
				{
					var body = (SelectQuery?)Visit(element.Body);

					if (ShouldReplace(element) || !ReferenceEquals(element.Body, body))
					{
						var clonedFields = new List<SqlField>(element.Fields.Count);
						for (var index = 0; index < element.Fields.Count; index++)
						{
							var field    = element.Fields[index];
							var newField = new SqlField(field);
							clonedFields[index] = newField;

							NotifyReplaced(newField, field);
						}

						var newCte = new CteClause(
							body,
							clonedFields,
							element.ObjectType,
							element.IsRecursive,
							element.Name);

						var correctedBody = body == null
							? null
							: (SelectQuery)new QueryElementCorrectVisitor(element, newCte).Visit(body);

						newCte.Body = correctedBody;
						return NotifyReplaced(newCte, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlSetOperator(SqlSetOperator element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.SelectQuery);
					break;
				}
				case VisitMode.Modify:
				{
					var selectQuery = (SelectQuery)Visit(element.SelectQuery);

					element.Modify(selectQuery);

					break;
				}
				case VisitMode.Transform:
				{
					var selectQuery = (SelectQuery)Visit(element.SelectQuery);

					if (ShouldReplace(element) || !ReferenceEquals(element.SelectQuery, selectQuery))
					{
						return NotifyReplaced(new SqlSetOperator(selectQuery, element.Operation), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlOrderByItem(SqlOrderByItem element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Expression);

					break;
				}
				case VisitMode.Modify:
				{
					element.Expression = (ISqlExpression)Visit(element.Expression);

					break;
				}
				case VisitMode.Transform:
				{
					var e = (ISqlExpression)Visit(element.Expression);

					if (ShouldReplace(element) || !ReferenceEquals(element.Expression, e))
						return NotifyReplaced(new SqlOrderByItem(e, element.IsDescending), element);

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlOrderByClause(SqlOrderByClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Items, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					VisitElements(element.Items, VisitMode.Modify);

					element.Modify(element.Items);

					break;
				}
				case VisitMode.Transform:
				{
					var items = VisitElements(element.Items, VisitMode.Transform);

					if (ShouldReplace(element) || items != null && !ReferenceEquals(element.Items, items))
					{
						return NotifyReplaced(new SqlOrderByClause(items ?? element.Items), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlGroupByClause(SqlGroupByClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Items, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					VisitElements(element.Items, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var items = VisitElements(element.Items, VisitMode.Transform);

					if (ShouldReplace(element) || items != null && !ReferenceEquals(element.Items, items))
					{
						return NotifyReplaced(new SqlGroupByClause(element.GroupingType, items ?? element.Items), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}
			
			return element;
		}

		public virtual IQueryElement VisitSqlWhereClause(SqlWhereClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.SearchCondition);

					break;
				}
				case VisitMode.Modify:
				{
					element.SearchCondition = (SqlSearchCondition)Visit(element.SearchCondition);
					
					break;
				}
				case VisitMode.Transform:
				{
					var searchCond = (SqlSearchCondition)Visit(element.SearchCondition);

					if (ShouldReplace(element) || !ReferenceEquals(element.SearchCondition, searchCond))
					{
						return NotifyReplaced(new SqlWhereClause(searchCond), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlFromClause(SqlFromClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Tables, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					VisitElements(element.Tables, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var tables = VisitElements(element.Tables, VisitMode.Transform);

					if (ShouldReplace(element) || tables != null && !ReferenceEquals(element.Tables, tables))
					{
						return NotifyReplaced(new SqlFromClause(tables ?? element.Tables), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlSetExpression(SqlSetExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Expression);
					Visit(element.Column);

					break;
				}
				case VisitMode.Modify:
				{
					element.Expression = (ISqlExpression?)Visit(element.Expression);
					element.Column     = (ISqlExpression)Visit(element.Column);

					break;
				}
				case VisitMode.Transform:
				{
					var expr   = (ISqlExpression?)Visit(element.Expression);
					var column = (ISqlExpression)Visit(element.Column);

					if (ShouldReplace(element) || !ReferenceEquals(element.Column, column) || !ReferenceEquals(element.Expression, expr))
					{
						return NotifyReplaced(new SqlSetExpression(column, expr), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlUpdateClause(SqlUpdateClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Table);
					Visit(element.TableSource);

					VisitElements(element.Items, VisitMode.ReadOnly);
					VisitElements(element.Keys, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					var table = (SqlTable?)Visit(element.Table);
					var ts    = (SqlTableSource?)Visit(element.TableSource);
					var items = VisitElements(element.Items, VisitMode.Modify)!;
					var keys  = VisitElements(element.Keys, VisitMode.Modify)!;

					element.Modify(table, ts, items, keys);

					break;
				}
				case VisitMode.Transform:
				{
					var table = (SqlTable?)Visit(element.Table);
					var ts    = (SqlTableSource?)Visit(element.TableSource);
					var items = VisitElements(element.Items, VisitMode.Transform);
					var keys  = VisitElements(element.Keys, VisitMode.Transform);

					if (ShouldReplace(element)                                    ||
					    !ReferenceEquals(element.Table, table)                    ||
					    !ReferenceEquals(element.TableSource, ts)                 ||
					    (items != null && !ReferenceEquals(element.Items, items)) ||
					    keys  != null && !ReferenceEquals(element.Keys, keys))
					{
						var newUpdate = new SqlUpdateClause { Table = table, TableSource = ts };

						newUpdate.Items.AddRange(items ?? element.Items);
						newUpdate.Keys.AddRange(keys   ?? element.Keys);

						return NotifyReplaced(newUpdate, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlInsertClause(SqlInsertClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Into);
					VisitElements(element.Items, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					var into = (SqlTable?)Visit(element.Into);
					VisitElements(element.Items, VisitMode.Modify);
					
					element.Modify(into, element.Items);
					
					break;
				}
				case VisitMode.Transform:
				{
					var into  = (SqlTable?)Visit(element.Into);
					var items = VisitElements(element.Items, VisitMode.Transform);

					if (ShouldReplace(element)               ||
					    !ReferenceEquals(element.Into, into) ||
					    (items != null && !ReferenceEquals(element.Items, items)))
					{
						var newInsert = new SqlInsertClause { Into = into };

						newInsert.Items.AddRange(items ?? element.Items);
						newInsert.WithIdentity = element.WithIdentity;

						return NotifyReplaced(newInsert, element);
					}
					
					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Table);
					Visit(element.Condition);

					break;
				}
				case VisitMode.Modify:
				{
					element.Table     = (SqlTableSource)Visit(element.Table);
					element.Condition = (SqlSearchCondition)Visit(element.Condition);

					break;
				}
				case VisitMode.Transform:
				{
					var table = (SqlTableSource)Visit(element.Table);
					var cond  = (SqlSearchCondition)Visit(element.Condition);

					if (ShouldReplace(element)                 ||
					    !ReferenceEquals(table, element.Table) ||
					    !ReferenceEquals(cond, element.Condition))
					{
						return NotifyReplaced(new SqlJoinedTable(element.JoinType, table, element.IsWeak, cond), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Source);
					VisitElements(element.Joins, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					var source = (ISqlTableSource)Visit(element.Source);
					var joins  = VisitElements(element.Joins, VisitMode.Modify)!;

					List<ISqlExpression[]>? uk = null;
					if (element.HasUniqueKeys)
						uk = VisitListOfArrays(element.UniqueKeys, VisitMode.Transform);

					element.Modify(source, joins, uk);

					break;
				}
				case VisitMode.Transform:
				{
					var source = (ISqlTableSource)Visit(element.Source);
					var joins  = VisitElements(element.Joins, VisitMode.Transform);

					List<ISqlExpression[]>? uk = null;
					if (element.HasUniqueKeys)
						uk = VisitListOfArrays(element.UniqueKeys, VisitMode.Transform);

					if (ShouldReplace(element)                   ||
					    !ReferenceEquals(source, element.Source) ||
					    (joins != null && !ReferenceEquals(element.Joins, joins)))
					{
						return NotifyReplaced(new SqlTableSource(
								source,
								element.RawAlias,
								joins ?? element.Joins,
								uk    ?? (element.HasUniqueKeys ? element.UniqueKeys : null)),
							element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlCondition(SqlCondition element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Predicate);
					break;
				}
				case VisitMode.Modify:
				{
					element.Predicate = (ISqlPredicate)Visit(element.Predicate);
					break;
				}
				case VisitMode.Transform:
				{
					var p = (ISqlPredicate)Visit(element.Predicate);
					if (ShouldReplace(element) || !ReferenceEquals(element.Predicate, p))
					{
						return NotifyReplaced(new SqlCondition(element.IsNot, p, element.IsOr), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}
			
			return element;
		}

		IQueryElement VisitSqlSearchCondition(SqlSearchCondition element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Conditions, VisitMode.ReadOnly);
					break;
				}
				case VisitMode.Modify:
				{
					var conditions = VisitElements(element.Conditions, VisitMode.Modify);

					element.Modify(conditions!);

					break;
				}
				case VisitMode.Transform:
				{
					var conditions = VisitElements(element.Conditions, VisitMode.Transform);

					if (ShouldReplace(element) || conditions != null && !ReferenceEquals(element.Conditions, conditions))
					{
						return NotifyReplaced(new SqlSearchCondition(conditions ?? element.Conditions), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlSelectClause(SqlSelectClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.TakeValue);
					Visit(element.SkipValue);
					foreach (var column in element.Columns)
					{
						VisitSqlColumnExpression(column, column.Expression);
					}

					break;
				}
				case VisitMode.Modify:
				{
					element.TakeValue = (ISqlExpression?)Visit(element.TakeValue);
					element.SkipValue = (ISqlExpression?)Visit(element.SkipValue);
					foreach (var column in element.Columns)
					{
						column.Expression = VisitSqlColumnExpression(column, column.Expression);
					}

					break;
				}
				case VisitMode.Transform:
				{
					var take = (ISqlExpression?)Visit(element.TakeValue);
					var skip = (ISqlExpression?)Visit(element.SkipValue);

					var modified = false;
					var expressions = new ISqlExpression[element.Columns.Count];
					for (var i = 0; i < element.Columns.Count; i++)
					{
						var column = element.Columns[i];
						var expr = VisitSqlColumnExpression(column, column.Expression);
						if (!ReferenceEquals(expr, column.Expression))
							modified = true;

						expressions[i] = expr;
					}

					if (ShouldReplace(element)                    ||
					    modified                                  ||
					    !ReferenceEquals(element.TakeValue, take) ||
					    !ReferenceEquals(element.SkipValue, skip))
					{
						var cols = new List<SqlColumn>(element.Columns.Count);

						for (var index = 0; index < element.Columns.Count; index++)
						{
							var oldColumn = element.Columns[index];
							var newColumn = new SqlColumn(element.SelectQuery, expressions[index], oldColumn.RawAlias);
							NotifyReplaced(newColumn, oldColumn);
							cols.Add(newColumn);
						}

						return NotifyReplaced(new SqlSelectClause(element.IsDistinct, take, element.TakeHints, skip, cols), element);
					}			

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlColumnReference(SqlColumn element) => element;

		public virtual ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			return (ISqlExpression)Visit(expression);
		}

		public virtual IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			switch (GetVisitMode(selectQuery))
			{
				case VisitMode.ReadOnly:
				{
					Visit(selectQuery.From   );
					Visit(selectQuery.Select );
					Visit(selectQuery.Where  );
					Visit(selectQuery.GroupBy);
					Visit(selectQuery.Having );
					Visit(selectQuery.OrderBy);

					if (selectQuery.HasSetOperators)
					{
						VisitElements(selectQuery.SetOperators, VisitMode.ReadOnly);
					}

					if (selectQuery.HasUniqueKeys)
						VisitListOfArrays(selectQuery.UniqueKeys, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					Visit(selectQuery.From   );
					Visit(selectQuery.Select );
					Visit(selectQuery.Where  );
					Visit(selectQuery.GroupBy);
					Visit(selectQuery.Having );
					Visit(selectQuery.OrderBy);

					if (selectQuery.HasSetOperators)
					{
						var so = VisitElements(selectQuery.SetOperators, VisitMode.Modify);
						if (so != null)
							selectQuery.SetOperators = so;
					}

					if (selectQuery.HasUniqueKeys)
					{
						var uk = VisitListOfArrays(selectQuery.UniqueKeys, VisitMode.Modify);
						if (uk != null ) 
							selectQuery.UniqueKeys = uk;
					}

					selectQuery.ParentSelect = (SelectQuery?)Visit(selectQuery.ParentSelect);

					break;
				}
				case VisitMode.Transform:
				{
					var fc = (SqlFromClause)   Visit(selectQuery.From   );

					var sc = (SqlSelectClause) Visit(selectQuery.Select );
					var wc = (SqlWhereClause)  Visit(selectQuery.Where  );
					var gc = (SqlGroupByClause)Visit(selectQuery.GroupBy);
					var hc = (SqlWhereClause)  Visit(selectQuery.Having );
					var oc = (SqlOrderByClause)Visit(selectQuery.OrderBy);


					List<SqlSetOperator>?   so = null;
					List<ISqlExpression[]>? uk = null;

					if (selectQuery.HasSetOperators)
						so = VisitElements(selectQuery.SetOperators, VisitMode.Transform);

					if (selectQuery.HasUniqueKeys)
						uk = VisitListOfArrays(selectQuery.UniqueKeys, VisitMode.Transform);

					if (ShouldReplace(selectQuery) 
					    || !ReferenceEquals(fc, selectQuery.From)
					    || !ReferenceEquals(sc, selectQuery.Select)
					    || !ReferenceEquals(wc, selectQuery.Where)
					    || !ReferenceEquals(gc, selectQuery.GroupBy)
					    || !ReferenceEquals(hc, selectQuery.Having)
					    || !ReferenceEquals(oc, selectQuery.OrderBy)
					    || selectQuery.HasSetOperators && so != null && !ReferenceEquals(so, selectQuery.SetOperators)
					    || selectQuery.HasUniqueKeys   && uk != null && !ReferenceEquals(uk, selectQuery.UniqueKeys)
					   )
					{
						var nq = new SelectQuery();

						if (ReferenceEquals(fc, selectQuery.From))
						{
							fc = new SqlFromClause(nq);
							fc.Tables.AddRange(selectQuery.From.Tables);

							NotifyReplaced(fc, selectQuery.From);
						}

						if (ReferenceEquals(sc, selectQuery.Select))
						{
							sc = new SqlSelectClause(selectQuery.Select.IsDistinct, selectQuery.Select.TakeValue,
								selectQuery.Select.TakeHints, selectQuery.Select.SkipValue,
								selectQuery.Select.Columns.Select(c => new SqlColumn(nq, c.Expression, c.RawAlias)));

							NotifyReplaced(sc, selectQuery.Select);
						}
						else
						{
							// all columns already copied, just reassign
							foreach (var c in sc.Columns) 
								c.Parent = nq;
						}

						if (ReferenceEquals(wc, selectQuery.Where))
						{
							wc                 = new SqlWhereClause(nq);
							wc.SearchCondition = selectQuery.Where.SearchCondition;

							NotifyReplaced(wc, selectQuery.Where);
						}

						if (ReferenceEquals(gc, selectQuery.GroupBy))
						{
							gc = new SqlGroupByClause(nq)
							{
								GroupingType = selectQuery.GroupBy.GroupingType
							};
							gc.Items.AddRange(selectQuery.GroupBy.Items);

							NotifyReplaced(gc, selectQuery.GroupBy);
						}

						if (ReferenceEquals(hc, selectQuery.Having))
						{
							hc                 = new SqlWhereClause(nq);
							hc.SearchCondition = selectQuery.Having.SearchCondition;

							NotifyReplaced(hc, selectQuery.Having);
						}

						if (ReferenceEquals(oc, selectQuery.OrderBy))
						{
							oc = new SqlOrderByClause(nq);
							oc.Items.AddRange(selectQuery.OrderBy.Items);

						    NotifyReplaced(oc, selectQuery.OrderBy);
						}

						if (selectQuery.HasSetOperators && !ReferenceEquals(so, selectQuery.SetOperators))
							so = new List<SqlSetOperator>(so ?? selectQuery.SetOperators);

						if (selectQuery.HasUniqueKeys && !ReferenceEquals(uk, selectQuery.UniqueKeys))
							uk = new List<ISqlExpression[]>(uk ?? selectQuery.UniqueKeys);

						nq.Init(sc, fc, wc, gc, hc, oc, so, uk,
							selectQuery.ParentSelect,
							selectQuery.IsParameterDependent,
							selectQuery.QueryName,
							selectQuery.DoNotSetAliases);

						return NotifyReplaced(nq, selectQuery);
					}

					break;
				}					
				default:
					throw CreateInvalidVisitModeException();
			}

			return selectQuery;
		}

		IQueryElement VisitFuncLikePredicate(SqlPredicate.FuncLike element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Function);
					break;
				}
				case VisitMode.Modify:
				{
					var func = Visit(element.Function);

					if (!ReferenceEquals(func, element.Function))
					{
						if (func is SqlFunction function)
						{
							element.Modify(function);
						}
						else if (func is ISqlPredicate predicate)
							return predicate;
						else
							throw new InvalidCastException("Converted FuncLikePredicate expression is not a Predicate expression.");
					}

					break;
				}
				case VisitMode.Transform:
				{
					var func = Visit(element.Function);

					if (ShouldReplace(element) || !ReferenceEquals(func, element.Function))
					{
						if (func is SqlFunction function)
							return NotifyReplaced(new SqlPredicate.FuncLike(function), element);
						if (func is ISqlPredicate predicate)
							return predicate;
						throw new InvalidCastException("Converted FuncLikePredicate expression is not a Predicate expression.");
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitInListPredicate(SqlPredicate.InList predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
				{
					Visit(predicate.Expr1);
					VisitElements(predicate.Values, VisitMode.ReadOnly);
					break;
				}
				case VisitMode.Modify:
				{
					var expr1  = (ISqlExpression)Visit(predicate.Expr1);
					var values = VisitElements(predicate.Values, VisitMode.Modify)!;

					predicate.Modify(expr1, values);

					break;
				}
				case VisitMode.Transform:
				{
					var expr1 = (ISqlExpression)Visit(predicate.Expr1);
					var values = VisitElements(predicate.Values, VisitMode.Transform);

					if (ShouldReplace(predicate)                 ||
					    !ReferenceEquals(predicate.Expr1, expr1) ||
					    (values != null && !ReferenceEquals(predicate.Values, values)))
					{
						return NotifyReplaced(new SqlPredicate.InList(expr1, predicate.WithNull, predicate.IsNot,
							values ?? predicate.Values), predicate);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;			
		}

		IQueryElement VisitInSubQueryPredicate(SqlPredicate.InSubQuery predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
				{
					Visit(predicate.Expr1);
					Visit(predicate.SubQuery);
					break;
				}
				case VisitMode.Modify:
				{
					var expr1    = (ISqlExpression)Visit(predicate.Expr1);
					var subQuery = (SelectQuery)Visit(predicate.SubQuery);

					predicate.Modify(expr1, subQuery);

					break;
				}
				case VisitMode.Transform:
				{
					var expr1    = (ISqlExpression)Visit(predicate.Expr1);
					var subQuery = (SelectQuery)Visit(predicate.SubQuery);

					if (ShouldReplace(predicate)                 ||
					    !ReferenceEquals(predicate.Expr1, expr1) ||
					    !ReferenceEquals(predicate.SubQuery, subQuery))
					{
						return NotifyReplaced(new SqlPredicate.InSubQuery(expr1, predicate.IsNot, subQuery), predicate);
					}
					
					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}
			
			return predicate;
		}

		public virtual IQueryElement VisitIsTruePredicate(SqlPredicate.IsTrue predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
				{
					Visit(predicate.Expr1);
					Visit(predicate.TrueValue);
					Visit(predicate.FalseValue);
					break;
				}
				case VisitMode.Modify:
				{
					predicate.Expr1      = (ISqlExpression)Visit(predicate.Expr1);
					predicate.TrueValue  = (ISqlExpression)Visit(predicate.TrueValue);
					predicate.FalseValue = (ISqlExpression)Visit(predicate.FalseValue);
					break;
				}
				case VisitMode.Transform:
				{
					var expr1      = (ISqlExpression)Visit(predicate.Expr1);
					var trueValue  = (ISqlExpression)Visit(predicate.TrueValue);
					var falseValue = (ISqlExpression)Visit(predicate.FalseValue);

					if (ShouldReplace(predicate)                         ||
					    !ReferenceEquals(predicate.Expr1, expr1)         ||
					    !ReferenceEquals(predicate.TrueValue, trueValue) ||
					    !ReferenceEquals(predicate.FalseValue, falseValue))

					{
						return NotifyReplaced(new SqlPredicate.IsTrue(expr1, trueValue, falseValue, predicate.WithNull, predicate.IsNot), predicate);
					}
					
					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		IQueryElement VisitIsDistinctPredicate(SqlPredicate.IsDistinct predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
				{
					Visit(predicate.Expr1);
					Visit(predicate.Expr2);
					break;
				}
				case VisitMode.Modify:
				{
					predicate.Expr1 = (ISqlExpression)Visit(predicate.Expr1);
					predicate.Expr2 = (ISqlExpression)Visit(predicate.Expr2);
					break;
				}
				case VisitMode.Transform:
				{
					var e1 = (ISqlExpression)Visit(predicate.Expr1);
					var e2 = (ISqlExpression)Visit(predicate.Expr2);

					if (!ReferenceEquals(predicate.Expr1, e1) || !ReferenceEquals(predicate.Expr2, e2))
						return NotifyReplaced(new SqlPredicate.IsDistinct(e1, predicate.IsNot, e2), predicate);

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;			
		}

		public virtual IQueryElement VisitIsNullPredicate(SqlPredicate.IsNull predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
				{
					Visit(predicate.Expr1);
					break;
				}
				case VisitMode.Modify:
				{
					predicate.Expr1 = (ISqlExpression)Visit(predicate.Expr1);
					break;
				}
				case VisitMode.Transform:
				{
					var e = (ISqlExpression)Visit(predicate.Expr1);

					if (ShouldReplace(predicate) || !ReferenceEquals(predicate.Expr1, e))
					{
						return NotifyReplaced(new SqlPredicate.IsNull(e, predicate.IsNot), predicate);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		public virtual IQueryElement VisitBetweenPredicate(SqlPredicate.Between predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
				{
					Visit(predicate.Expr1);
					Visit(predicate.Expr2);
					Visit(predicate.Expr3);

					break;
				}
				case VisitMode.Modify:
				{
					predicate.Expr1 = (ISqlExpression)Visit(predicate.Expr1);
					predicate.Expr2 = (ISqlExpression)Visit(predicate.Expr2);
					predicate.Expr3 = (ISqlExpression)Visit(predicate.Expr3);

					break;
				}
				case VisitMode.Transform:
				{
					var expr1 = (ISqlExpression)Visit(predicate.Expr1);
					var expr2 = (ISqlExpression)Visit(predicate.Expr2);
					var expr3 = (ISqlExpression)Visit(predicate.Expr3);

					if (ShouldReplace(predicate) || !ReferenceEquals(predicate.Expr1, expr1) || !ReferenceEquals(predicate.Expr2, expr2) || !ReferenceEquals(predicate.Expr3, expr3))
					{
						return NotifyReplaced(new SqlPredicate.Between(expr1, predicate.IsNot, expr2, expr3), predicate);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		public virtual IQueryElement VisitSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
				{
					Visit(predicate.Expr1);
					Visit(predicate.Expr2);
					Visit(predicate.CaseSensitive);
					break;
				}
				case VisitMode.Modify:
				{
					var expr1         = (ISqlExpression)Visit(predicate.Expr1);
					var expr2         = (ISqlExpression)Visit(predicate.Expr2);
					var caseSensitive = (ISqlExpression)Visit(predicate.CaseSensitive);

					predicate.Modify(expr1, expr2, caseSensitive);

					break;
				}
				case VisitMode.Transform:
				{
					var expr1 = (ISqlExpression)Visit(predicate.Expr1);
					var expr2 = (ISqlExpression)Visit(predicate.Expr2);
					var caseSensitive = (ISqlExpression)Visit(predicate.CaseSensitive);

					if (ShouldReplace(predicate)                 || 
					    !ReferenceEquals(predicate.Expr1, expr1) || 
					    !ReferenceEquals(predicate.Expr2, expr2) || 
					    !ReferenceEquals(predicate.CaseSensitive, caseSensitive))
					{
						return NotifyReplaced(
							new SqlPredicate.SearchString(expr1, predicate.IsNot, expr2, predicate.Kind, caseSensitive),
							predicate);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		public virtual IQueryElement VisitLikePredicate(SqlPredicate.Like predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
				{
					Visit(predicate.Expr1);
					Visit(predicate.Expr2);
					if (predicate.Escape != null)
						Visit(predicate.Escape);
					break;
				}	
				case VisitMode.Modify:
				{
					predicate.Expr1 = (ISqlExpression)Visit(predicate.Expr1);
					predicate.Expr2 = (ISqlExpression)Visit(predicate.Expr2);
					if (predicate.Escape != null)
						predicate.Escape = (ISqlExpression)Visit(predicate.Escape);
					break;
				}	
				case VisitMode.Transform:
				{
					var e1  = (ISqlExpression)Visit(predicate.Expr1);
					var e2  = (ISqlExpression)Visit(predicate.Expr2);
					var esc = predicate.Escape != null ? (ISqlExpression)Visit(predicate.Escape) : null;

					if (ShouldReplace(predicate)              || !ReferenceEquals(predicate.Expr1, e1) ||
					    !ReferenceEquals(predicate.Expr2, e2) || !ReferenceEquals(predicate.Escape, esc))
					{
						return NotifyReplaced(new SqlPredicate.Like(e1, predicate.IsNot, e2, esc, predicate.FunctionName), predicate);
					}

					break;
				}	
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		public virtual IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
				{
					Visit(predicate.Expr1);
					Visit(predicate.Expr2);
					break;
				}
				case VisitMode.Modify:
				{
					predicate.Expr1 = (ISqlExpression)Visit(predicate.Expr1);
					predicate.Expr2 = (ISqlExpression)Visit(predicate.Expr2);
					break;
				}
				case VisitMode.Transform:
				{
					var expr1 = (ISqlExpression)Visit(predicate.Expr1);
					var expr2 = (ISqlExpression)Visit(predicate.Expr2);

					if (ShouldReplace(predicate) || !ReferenceEquals(predicate.Expr1, expr1) || !ReferenceEquals(predicate.Expr2, expr2))
					{
						return NotifyReplaced(new SqlPredicate.ExprExpr(expr1, predicate.Operator, expr2, predicate.WithNull), predicate);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		public virtual IQueryElement VisitNotExprPredicate(SqlPredicate.NotExpr predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
					Visit(predicate.Expr1);
					break;
				case VisitMode.Modify:
					predicate.Expr1 = (ISqlExpression)Visit(predicate.Expr1);
					break;
				case VisitMode.Transform:
				{
					var e = (ISqlExpression)Visit(predicate.Expr1);

					if (ShouldReplace(predicate) ||!ReferenceEquals(predicate.Expr1, e))
					{
						return NotifyReplaced(new SqlPredicate.NotExpr(e, predicate.IsNot, predicate.Precedence), predicate);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		public virtual IQueryElement VisitExprPredicate(SqlPredicate.Expr predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
				{
					Visit(predicate.Expr1);
					break;
				}
				case VisitMode.Modify:
				{
					predicate.Expr1 = (ISqlExpression)Visit(predicate.Expr1);
					break;
				}
				case VisitMode.Transform:
				{
					var e = (ISqlExpression)Visit(predicate.Expr1);

					if (ShouldReplace(predicate) || !ReferenceEquals(predicate.Expr1, e))
					{
						return NotifyReplaced(new SqlPredicate.Expr(e, predicate.Precedence), predicate);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		public virtual IQueryElement VisitSqlRow(SqlRow element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					foreach (var value in element.Values)
						Visit(value);
					break;
				}
				case VisitMode.Modify:
				{
					for (var i = 0; i < element.Values.Length; i++)
					{
						element.Values[i] = (ISqlExpression)Visit(element.Values[i]);
					}

					break;
				}	
				case VisitMode.Transform:
				{
					var values = VisitElements(element.Values, VisitMode.Transform);

					if (values != null && !ReferenceEquals(element.Values, values))
					{
						return NotifyReplaced(new SqlRow(values), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlAliasPlaceholder(SqlAliasPlaceholder element)
		{
			if (GetVisitMode(element) == VisitMode.Transform && ShouldReplace(element))
				return NotifyReplaced(new SqlAliasPlaceholder(), element);

			return element;
		}

		public virtual IQueryElement VisitSqlTable(SqlTable element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					break;
				}
				case VisitMode.Modify:
				{
					break;
				}
				case VisitMode.Transform:
				{
					if (ShouldReplace(element))
					{
						var newTable = new SqlTable(element);
						for (var index = 0; index < newTable.Fields.Count; index++)
						{
							NotifyReplaced(newTable.Fields[index], element.Fields[index]);
						}

						return NotifyReplaced(newTable, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlDataType(SqlDataType element) => element;

		public virtual IQueryElement VisitSqlValue(SqlValue element) => element;

		public virtual IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
		{
			var expr1 = (ISqlExpression)Visit(element.Expr1);
			var expr2 = (ISqlExpression)Visit(element.Expr2);

			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					break;
				}
				case VisitMode.Modify:
				{
					element.Expr1 = expr1;
					element.Expr2 = expr2;
					break;
				}	
				case VisitMode.Transform:
				{
					if (ShouldReplace(element)                 ||
					    !ReferenceEquals(expr1, element.Expr1) ||
					    !ReferenceEquals(expr2, element.Expr2))
					{
						return NotifyReplaced(new SqlBinaryExpression(
								element.SystemType,
								expr1,
								element.Operation,
								expr2,
								element.Precedence),
							element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlObjectExpression(SqlObjectExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					foreach (var t in element.InfoParameters)
					{
						Visit(t.Sql);
					}

					break;
				}
				case VisitMode.Modify:
				{
					for (int i = 0; i < element.InfoParameters.Length; i++)
					{
						var sqlInfo = element.InfoParameters[i];

						element.InfoParameters[i] = sqlInfo.WithSql((ISqlExpression)Visit(sqlInfo.Sql));
					}

					break;
				}
				case VisitMode.Transform:
				{
					SqlGetValue[]? currentParams = null;

					for (int i = 0; i < element.InfoParameters.Length; i++)
					{
						var sqlInfo = element.InfoParameters[i];

						var newExpr = (ISqlExpression)Visit(sqlInfo.Sql);

						if (!ReferenceEquals(newExpr, sqlInfo.Sql))
						{
							if (currentParams == null)
							{
								currentParams = new SqlGetValue[element.InfoParameters.Length];
								Array.Copy(element.InfoParameters, currentParams, i);
							}

							var newInfo = sqlInfo.WithSql(newExpr);
							currentParams[i] = newInfo;
						}
						else if (currentParams != null)
							currentParams[i] = sqlInfo;
					}

					if (ShouldReplace(element) || currentParams != null)
						return NotifyReplaced(new SqlObjectExpression(element.MappingSchema, currentParams ?? element.InfoParameters), element);

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlAnchor(SqlAnchor element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.SqlExpression);
					break;
				}
				case VisitMode.Modify:
				{
					element.Modify((ISqlExpression)Visit(element.SqlExpression));
					break;
				}
				case VisitMode.Transform:
				{
					var sqlExpr = (ISqlExpression)Visit(element.SqlExpression);

					if (ShouldReplace(element) || !ReferenceEquals(sqlExpr, element.SqlExpression))
						return NotifyReplaced(new SqlAnchor(sqlExpr, element.AnchorKind), element);

					break;
				}	
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlNullabilityExpression(SqlNullabilityExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.SqlExpression);
					break;
				}
				case VisitMode.Modify:
				{
					var sqlExpr = (ISqlExpression)Visit(element.SqlExpression);

					element.Modify(sqlExpr);

					break;
				}
				case VisitMode.Transform:
				{
					var sqlExpr = (ISqlExpression)Visit(element.SqlExpression);

					if (ShouldReplace(element) ||!ReferenceEquals(sqlExpr, element.SqlExpression))
						return NotifyReplaced(new SqlNullabilityExpression(sqlExpr), element);

					break;
				}	
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlExpression(SqlExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Parameters, VisitMode.ReadOnly);
					break;
				}
				case VisitMode.Modify:
				{
					VisitElements(element.Parameters, VisitMode.Modify);
					break;
				}
				case VisitMode.Transform:
				{
					var parameters = VisitElements(element.Parameters, VisitMode.Transform);
					if (ShouldReplace(element) ||
					    parameters != null && !ReferenceEquals(parameters, element.Parameters))
					{
						return NotifyReplaced(new SqlExpression(
							element.SystemType, element.Expr, element.Precedence,
							element.Flags, element.NullabilityType, element.CanBeNullNullable, parameters ?? element.Parameters), 
							element);
					}

					break;
				}	
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		public virtual IQueryElement VisitSqlParameter(SqlParameter sqlParameter) => sqlParameter;

		public virtual IQueryElement VisitSqlFunction(SqlFunction element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Parameters, VisitMode.ReadOnly);
					break;
				}
				case VisitMode.Modify:
				{
					VisitElements(element.Parameters, VisitMode.Modify);
					break;
				}
				case VisitMode.Transform:
				{
					var parameters = VisitElements(element.Parameters, VisitMode.Transform);
					if (ShouldReplace(element) || parameters != null && !ReferenceEquals(parameters, element.Parameters))
					{
						return NotifyReplaced(
							new SqlFunction(element.SystemType, element.Name, element.IsAggregate,
								element.IsPure,
								element.Precedence, element.NullabilityType, element.CanBeNullNullable, parameters ?? element.Parameters)
							{
								DoNotOptimize = element.DoNotOptimize
							},
							element);
					}

					break;
				}	
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		#region Helper functions

		protected Exception CreateInvalidVisitModeException([CallerMemberName] string? methodName = null)
			=> new InvalidOperationException($"Invalid VisitMode in '{methodName}'");

		protected T[]? VisitElements<T>(T[] arr1, VisitMode mode)
			where T : class, IQueryElement
		{
			switch (mode)
			{
				case VisitMode.Modify:
				{
					for (var i = 0; i < arr1.Length; i++)
					{
						var elem = (T?)Visit(arr1[i]);
						if (elem != null)
							arr1[i] = elem;
					}

					return arr1;
				}

				case VisitMode.ReadOnly:
				{
					foreach (var t in arr1)
					{
						_ = Visit(t);
					}

					return arr1;
				}

				case VisitMode.Transform:
				{
					T[]? arr2 = null;

					for (var i = 0; i < arr1.Length; i++)
					{
						var elem1 = arr1[i];
						var elem2 = (T?)Visit(elem1);

						if (elem2 != null && !ReferenceEquals(elem1, elem2))
						{
							if (arr2 == null)
							{
								arr2 = new T[arr1.Length];

								for (var j = 0; j < i; j++)
									arr2[j] = arr1[j];
							}

							arr2[i] = elem2;
						}
						else if (arr2 != null)
							arr2[i] = elem1;
					}

					return arr2;
				}
				default:
					throw CreateInvalidVisitModeException();
			}
		}

		protected void VisitElementsReadOnly<T>(IEnumerable<T> elements)
			where T : class, IQueryElement
		{
			foreach (var t in elements)
			{
				_ = Visit(t);
			}
		}

		protected List<T>? VisitElements<T>(List<T> arr1, VisitMode mode)
			where T : class, IQueryElement
		{
			switch (mode)
			{
				case VisitMode.Modify:
				{
					for (var i = 0; i < arr1.Count; i++)
					{
						var elem = (T?)Visit(arr1[i]);
						if (elem != null)
							arr1[i] = elem;
					}

					return arr1;
				}

				case VisitMode.ReadOnly:
				{
					foreach (var t in arr1)
					{
						_ = Visit(t);
					}

					return arr1;
				}

				case VisitMode.Transform:
				{
					List<T>? arr2 = null;

					for (var i = 0; i < arr1.Count; i++)
					{
						var elem1 = arr1[i];
						var elem2 = (T?)Visit(elem1);

						if (elem2 != null && !ReferenceEquals(elem1, elem2))
						{
							if (arr2 == null)
							{
								arr2 = new List<T>(arr1.Count);

								for (var j = 0; j < i; j++)
									arr2.Add(arr1[j]);
							}

							arr2.Add(elem2);
						}
						else if (arr2 != null)
							arr2.Add(elem1);
					}

					return arr2;
				}

				default:
					throw CreateInvalidVisitModeException();
			}
		}

		protected List<T[]>? VisitListOfArrays<T>(List<T[]> list1, VisitMode mode)
			where T : class, IQueryElement
		{
			switch (mode)
			{
				case VisitMode.ReadOnly:
				{
					foreach (var t in list1)
					{
						VisitElements(t, VisitMode.ReadOnly);
					}

					return list1;
				}
				case VisitMode.Modify:
				{
					for (var i = 0; i < list1.Count; i++)
					{
						var elem = VisitElements(list1[i], VisitMode.Modify);
						if (elem != null)
							list1[i] = elem;
					}

					return list1;
				}
				case VisitMode.Transform:
				{
					List<T[]>? list2 = null;

					for (var i = 0; i < list1.Count; i++)
					{
						var elem1 = list1[i];
						var elem2 = VisitElements(elem1, VisitMode.Transform);

						if (elem2 != null && !ReferenceEquals(elem1, elem2))
						{
							if (list2 == null)
							{
								list2 = new List<T[]>(list1.Count);

								for (var j = 0; j < i; j++)
								{
									list2.Add(list1[j]);
								}
							}

							list2.Add(elem2);
						}
						else if (list2 != null)
						{
							list2.Add(elem1);
						}
					}

					return list2;
				}

				default:
					throw CreateInvalidVisitModeException();
			}
		}

		protected List<T[]>? VisitListOfArrays<T>(IReadOnlyList<T[]> list1, VisitMode mode)
			where T : class, IQueryElement
		{
			switch (mode)
			{
				case VisitMode.ReadOnly:
				{
					foreach (var t in list1)
					{
						VisitElements(t, VisitMode.ReadOnly);
					}

					return null;
				}

				case VisitMode.Modify:
				case VisitMode.Transform:
				{
					List<T[]>? list2 = null;

					for (var i = 0; i < list1.Count; i++)
					{
						var elem1 = list1[i];
						var elem2 = VisitElements(elem1, mode);

						if (elem2 != null && !ReferenceEquals(elem1, elem2))
						{
							if (list2 == null)
							{
								list2 = new List<T[]>(list1.Count);

								for (var j = 0; j < i; j++)
								{
									list2.Add(list1[j]);
								}
							}

							list2.Add(elem2);
						}
						else if (list2 != null)
						{
							list2.Add(elem1);
						}
					}

					return list2;
				}

				default:
					throw CreateInvalidVisitModeException();
			}
		}

		#endregion
	}

	public class QueryElementCorrectVisitor : QueryElementVisitor
	{
		readonly IQueryElement _toReplace;
		readonly IQueryElement _replaceBy;

		public QueryElementCorrectVisitor(IQueryElement toReplace, IQueryElement replaceBy) : base(VisitMode.Modify)
		{
			_toReplace = toReplace;
			_replaceBy = replaceBy;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (ReferenceEquals(element, _toReplace))
			{
				return _replaceBy;
			}

			return base.Visit(element);
		}
	}

}
