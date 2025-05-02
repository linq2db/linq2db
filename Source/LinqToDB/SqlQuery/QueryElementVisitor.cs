using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LinqToDB.SqlQuery
{
	// TODO: REFACTORING: it probably makes sense to move Visit calls from element visit switch to upper level to:
	// - reduce function/code size
	// - reduce chances to de-sync VisitMode branches on changes
	/// <summary>
	/// Base visitor for all SQL AST visitors.
	/// Supports three visit modes, defined by <see cref="VisitMode"/> enum.
	/// </summary>
	public abstract class QueryElementVisitor
	{
		protected QueryElementVisitor(VisitMode visitMode)
		{
			VisitMode = visitMode;
		}

		/// <summary>
		/// Gets default visitor inspection mode.
		/// </summary>
		public VisitMode VisitMode { get; }

		/// <summary>
		/// Gets visit mode for query element.
		/// Could be overridden to enable element visit mode, which differ from visitor-level mode set by <see cref="VisitMode"/>.
		/// </summary>
		public virtual VisitMode GetVisitMode(IQueryElement element) => VisitMode;

		/// <summary>
		/// Enables unconditional cloning (returning of new instance) of query element in <see cref="VisitMode.Transform"/>.
		/// Default implementation returns <c>false</c>.
		/// </summary>
		protected virtual bool ShouldReplace(IQueryElement element) => false;

		/// <summary>
		/// Called by visitor on node replacement in <see cref="VisitMode.Transform"/> mode.
		/// Descendant visitor could overload it to react to node cloning.
		/// </summary>
		/// <param name="newElement">New query element.</param>
		/// <param name="oldElement">Old query element.</param>
		/// <returns>Returns new element (override could create anoter copy).</returns>
		public virtual IQueryElement NotifyReplaced(IQueryElement newElement, IQueryElement oldElement) => newElement;

		/// <summary>
		/// Visitor dispatch method.
		/// </summary>
		[return: NotNullIfNotNull(nameof(element))]
		public virtual IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null)
				return element;

			return element.ElementType switch
			{
				QueryElementType.SqlField                  => VisitSqlFieldReference         ((SqlField                   )element),
				QueryElementType.SqlFunction               => VisitSqlFunction               ((SqlFunction                )element),
				QueryElementType.SqlParameter              => VisitSqlParameter              ((SqlParameter               )element),
				QueryElementType.SqlExpression             => VisitSqlExpression             ((SqlExpression              )element),
				QueryElementType.SqlNullabilityExpression  => VisitSqlNullabilityExpression  ((SqlNullabilityExpression   )element),
				QueryElementType.SqlAnchor                 => VisitSqlAnchor                 ((SqlAnchor                  )element),
				QueryElementType.SqlObjectExpression       => VisitSqlObjectExpression       ((SqlObjectExpression        )element),
				QueryElementType.SqlBinaryExpression       => VisitSqlBinaryExpression       ((SqlBinaryExpression        )element),
				QueryElementType.SqlValue                  => VisitSqlValue                  ((SqlValue                   )element),
				QueryElementType.SqlDataType               => VisitSqlDataType               ((SqlDataType                )element),
				QueryElementType.SqlTable                  => VisitSqlTable                  ((SqlTable                   )element),
				QueryElementType.SqlAliasPlaceholder       => VisitSqlAliasPlaceholder       ((SqlAliasPlaceholder        )element),
				QueryElementType.SqlRow                    => VisitSqlRow                    ((SqlRowExpression           )element),
				QueryElementType.NotPredicate              => VisitNotPredicate              ((SqlPredicate.Not           )element),
				QueryElementType.TruePredicate             => VisitTruePredicate             ((SqlPredicate.TruePredicate )element),
				QueryElementType.FalsePredicate            => VisitFalsePredicate            ((SqlPredicate.FalsePredicate)element),
				QueryElementType.ExprPredicate             => VisitExprPredicate             ((SqlPredicate.Expr          )element),
				QueryElementType.ExprExprPredicate         => VisitExprExprPredicate         ((SqlPredicate.ExprExpr      )element),
				QueryElementType.LikePredicate             => VisitLikePredicate             ((SqlPredicate.Like          )element),
				QueryElementType.SearchStringPredicate     => VisitSearchStringPredicate     ((SqlPredicate.SearchString  )element),
				QueryElementType.BetweenPredicate          => VisitBetweenPredicate          ((SqlPredicate.Between       )element),
				QueryElementType.IsNullPredicate           => VisitIsNullPredicate           ((SqlPredicate.IsNull        )element),
				QueryElementType.IsDistinctPredicate       => VisitIsDistinctPredicate       ((SqlPredicate.IsDistinct    )element),
				QueryElementType.IsTruePredicate           => VisitIsTruePredicate           ((SqlPredicate.IsTrue        )element),
				QueryElementType.InSubQueryPredicate       => VisitInSubQueryPredicate       ((SqlPredicate.InSubQuery    )element),
				QueryElementType.InListPredicate           => VisitInListPredicate           ((SqlPredicate.InList        )element),
				QueryElementType.ExistsPredicate           => VisitExistsPredicate           ((SqlPredicate.Exists        )element),
				QueryElementType.SqlQuery                  => VisitSqlQuery                  ((SelectQuery                )element),
				QueryElementType.Column                    => VisitSqlColumnReference        ((SqlColumn                  )element),
				QueryElementType.SearchCondition           => VisitSqlSearchCondition        ((SqlSearchCondition         )element),
				QueryElementType.TableSource               => VisitSqlTableSource            ((SqlTableSource             )element),
				QueryElementType.JoinedTable               => VisitSqlJoinedTable            ((SqlJoinedTable             )element),
				QueryElementType.SelectClause              => VisitSqlSelectClause           ((SqlSelectClause            )element),
				QueryElementType.InsertClause              => VisitSqlInsertClause           ((SqlInsertClause            )element),
				QueryElementType.UpdateClause              => VisitSqlUpdateClause           ((SqlUpdateClause            )element),
				QueryElementType.SetExpression             => VisitSqlSetExpression          ((SqlSetExpression           )element),
				QueryElementType.FromClause                => VisitSqlFromClause             ((SqlFromClause              )element),
				QueryElementType.WhereClause               => VisitSqlWhereClause            ((SqlWhereClause             )element),
				QueryElementType.HavingClause              => VisitSqlHavingClause           ((SqlHavingClause            )element),
				QueryElementType.GroupByClause             => VisitSqlGroupByClause          ((SqlGroupByClause           )element),
				QueryElementType.OrderByClause             => VisitSqlOrderByClause          ((SqlOrderByClause           )element),
				QueryElementType.OrderByItem               => VisitSqlOrderByItem            ((SqlOrderByItem             )element),
				QueryElementType.SetOperator               => VisitSqlSetOperator            ((SqlSetOperator             )element),
				QueryElementType.WithClause                => VisitSqlWithClause             ((SqlWithClause              )element),
				QueryElementType.CteClause                 => VisitCteClause                 ((CteClause                  )element),
				QueryElementType.SqlCteTable               => VisitSqlCteTable               ((SqlCteTable                )element),
				QueryElementType.SqlRawSqlTable            => VisitSqlRawSqlTable            ((SqlRawSqlTable             )element),
				QueryElementType.SqlValuesTable            => VisitSqlValuesTable            ((SqlValuesTable             )element),
				QueryElementType.OutputClause              => VisitSqlOutputClause           ((SqlOutputClause            )element),
				QueryElementType.SelectStatement           => VisitSqlSelectStatement        ((SqlSelectStatement         )element),
				QueryElementType.InsertStatement           => VisitSqlInsertStatement        ((SqlInsertStatement         )element),
				QueryElementType.InsertOrUpdateStatement   => VisitSqlInsertOrUpdateStatement((SqlInsertOrUpdateStatement )element),
				QueryElementType.UpdateStatement           => VisitSqlUpdateStatement        ((SqlUpdateStatement         )element),
				QueryElementType.DeleteStatement           => VisitSqlDeleteStatement        ((SqlDeleteStatement         )element),
				QueryElementType.MergeStatement            => VisitSqlMergeStatement         ((SqlMergeStatement          )element),
				QueryElementType.MultiInsertStatement      => VisitSqlMultiInsertStatement   ((SqlMultiInsertStatement    )element),
				QueryElementType.ConditionalInsertClause   => VisitSqlConditionalInsertClause((SqlConditionalInsertClause )element),
				QueryElementType.CreateTableStatement      => VisitSqlCreateTableStatement   ((SqlCreateTableStatement    )element),
				QueryElementType.DropTableStatement        => VisitSqlDropTableStatement     ((SqlDropTableStatement      )element),
				QueryElementType.TruncateTableStatement    => VisitSqlTruncateTableStatement ((SqlTruncateTableStatement  )element),
				QueryElementType.SqlTableLikeSource        => VisitSqlTableLikeSource        ((SqlTableLikeSource         )element),
				QueryElementType.MergeOperationClause      => VisitSqlMergeOperationClause   ((SqlMergeOperationClause    )element),
				QueryElementType.GroupingSet               => VisitSqlGroupingSet            ((SqlGroupingSet             )element),
				QueryElementType.Comment                   => VisitSqlComment                ((SqlComment                 )element),
				QueryElementType.SqlExtension              => VisitSqlExtension              ((IQueryExtension            )element),
				QueryElementType.SqlInlinedExpression      => VisitSqlInlinedSqlExpression   ((SqlInlinedSqlExpression    )element),
				QueryElementType.SqlInlinedToSqlExpression => VisitSqlInlinedToSqlExpression ((SqlInlinedToSqlExpression  )element),
				QueryElementType.SqlQueryExtension         => VisitSqlQueryExtension         ((SqlQueryExtension          )element),
				QueryElementType.SqlCondition              => VisitSqlConditionExpression    ((SqlConditionExpression     )element),
				QueryElementType.SqlCast                   => VisitSqlCastExpression         ((SqlCastExpression          )element),
				QueryElementType.SqlCoalesce               => VisitSqlCoalesceExpression     ((SqlCoalesceExpression      )element),
				QueryElementType.SqlCase                   => VisitSqlCaseExpression         ((SqlCaseExpression          )element),
				QueryElementType.CompareTo                 => VisitSqlCompareToExpression    ((SqlCompareToExpression     )element),

				_ => throw new InvalidOperationException()
			};
		}

		#region Query element VisitSqlXXX methods

		/// <summary>
		/// Main <see cref="CteClause"/> visitor is <see cref="VisitCteClause"/> and called for it from <see cref="SqlWithClause"/>.
		/// This by-ref visitor used for references from <see cref="SqlCteTable"/>.
		/// </summary>
		protected virtual IQueryElement VisitCteClauseReference(CteClause element) => element;

		/// <summary>
		/// Visitor of <see cref="CteClause"/> definition from <see cref="SqlWithClause"/> visitor (owner).
		/// For visitor of <see cref="CteClause"/> in queries see <see cref="VisitCteClauseReference"/> visitor.
		/// </summary>
		protected virtual IQueryElement VisitCteClause(CteClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Body);
					// TODO: currently needed for linq serializer and should be removed after serializer refactoring
					VisitElements(element.Fields, VisitMode.ReadOnly);
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
						var clonedFields = CopyFields(element.Fields);

						var newCte = new CteClause(
							body,
							clonedFields,
							element.ObjectType,
							element.IsRecursive,
							element.Name);

						return NotifyReplaced(newCte, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		/// <summary>
		/// Visitor for <see cref="SqlField"/> reference from query expressions.
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		protected virtual IQueryElement VisitSqlFieldReference(SqlField element) => element;

		/// <summary>
		/// Used to visit columns as references in other expressions.
		/// Actual visit of table column happens in <see cref="VisitSqlColumnExpression(SqlColumn, ISqlExpression)"/>.
		/// </summary>
		protected virtual IQueryElement VisitSqlColumnReference(SqlColumn element) => element;

		/// <summary>
		/// Visit of column expression from owner table. For column references visitor see <see cref="VisitSqlColumnReference"/>
		/// </summary>
		protected virtual ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			return (ISqlExpression)Visit(expression);
		}

		protected virtual IQueryElement VisitSqlInlinedSqlExpression(SqlInlinedSqlExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Parameter);
					Visit(element.InlinedValue);

					break;
				}
				case VisitMode.Modify:
				{
					var parameter    = (SqlParameter)Visit(element.Parameter);
					var inlinedValue = (ISqlExpression)Visit(element.InlinedValue);
					element.Modify(parameter, inlinedValue);

					break;
				}
				case VisitMode.Transform:
				{
					var parameter    = (SqlParameter)Visit(element.Parameter);
					var inlinedValue = (ISqlExpression)Visit(element.InlinedValue);

					if (ShouldReplace(element) || !ReferenceEquals(element.Parameter, parameter) || !ReferenceEquals(element.InlinedValue, inlinedValue))
					{
						return NotifyReplaced(new SqlInlinedSqlExpression(parameter, inlinedValue), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlInlinedToSqlExpression(SqlInlinedToSqlExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Parameter);
					Visit(element.InlinedValue);

					break;
				}
				case VisitMode.Modify:
				{
					var parameter    = (SqlParameter)Visit(element.Parameter);
					var inlinedValue = (ISqlExpression)Visit(element.InlinedValue);
					element.Modify(parameter, inlinedValue);

					break;
				}
				case VisitMode.Transform:
				{
					var parameter    = (SqlParameter)Visit(element.Parameter);
					var inlinedValue = (ISqlExpression)Visit(element.InlinedValue);

					if (ShouldReplace(element) || !ReferenceEquals(element.Parameter, parameter) || !ReferenceEquals(element.InlinedValue, inlinedValue))
					{
						return NotifyReplaced(new SqlInlinedToSqlExpression(parameter, inlinedValue), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		private IQueryElement VisitSqlExtension(IQueryExtension element)
		{
			return element.Accept(this);
		}

		protected virtual IQueryElement VisitSqlComment(SqlComment element) => element;

		protected virtual IQueryElement VisitSqlGroupingSet(SqlGroupingSet element)
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

					if (ShouldReplace(element) || element.Items != items)
					{
						return NotifyReplaced(new SqlGroupingSet(element.Items != items ? items : items.ToList()), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlMergeOperationClause(SqlMergeOperationClause element)
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
					    element.Items != items)
					{
						return NotifyReplaced(new SqlMergeOperationClause(
								element.OperationType,
								where,
								whereDelete,
								element.Items != items ? items : items.ToList()),
							element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlTableLikeSource(SqlTableLikeSource element)
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
						var newFields = CopyFields(element.SourceFields);

						return NotifyReplaced(new SqlTableLikeSource(
							element.SourceID,
							sourceEnumerable,
							sourceQuery,
							newFields), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlTruncateTableStatement(SqlTruncateTableStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.Table);

					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag   = (SqlComment?)Visit(element.Tag);
					element.Table = (SqlTable?)Visit(element.Table);

					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var tag   = (SqlComment?)Visit(element.Tag);
					var table = (SqlTable?)Visit(element.Table);
					var ext   = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)                 ||
					    !ReferenceEquals(element.Tag, tag)     ||
					    !ReferenceEquals(element.Table, table) ||
					    element.SqlQueryExtensions != ext)
					{
						return NotifyReplaced(
							new SqlTruncateTableStatement
							{
								Tag                = tag,
								Table              = table,
								ResetIdentity      = element.ResetIdentity,
								SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
							}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlDropTableStatement(SqlDropTableStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.Table);

					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag = (SqlComment?)Visit(element.Tag);
					var table   = (SqlTable)Visit(element.Table);

					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					element.Modify(table);

					break;
				}
				case VisitMode.Transform:
				{
					var tag   = (SqlComment?)Visit(element.Tag);
					var table = (SqlTable)Visit(element.Table);
					var ext   = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)                 ||
					    !ReferenceEquals(element.Tag, tag)     ||
					    !ReferenceEquals(element.Table, table) ||
					    element.SqlQueryExtensions != ext)
					{
						return NotifyReplaced(
							new SqlCreateTableStatement(table)
							{
								Tag                = tag,
								SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
							}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlCreateTableStatement(SqlCreateTableStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.Table);

					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag = (SqlComment?)Visit(element.Tag);
					var table   = (SqlTable)Visit(element.Table);

					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					element.Modify(table);

					break;
				}
				case VisitMode.Transform:
				{
					var tag   = (SqlComment?)Visit(element.Tag);
					var table = (SqlTable)Visit(element.Table);
					var ext   = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)                 ||
					    !ReferenceEquals(element.Tag, tag)     ||
					    !ReferenceEquals(element.Table, table) ||
					    element.SqlQueryExtensions != ext)
					{
						return NotifyReplaced(
							new SqlCreateTableStatement(table)
							{
								Tag                = tag,
								SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
							}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlConditionalInsertClause(SqlConditionalInsertClause element)
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

		protected virtual IQueryElement VisitSqlMultiInsertStatement(SqlMultiInsertStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Source);
					VisitElements(element.Inserts, VisitMode.ReadOnly);
					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					var source = (SqlTableLikeSource)Visit(element.Source);
					VisitElements(element.Inserts, VisitMode.Modify);
					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					element.Modify(source);

					break;
				}
				case VisitMode.Transform:
				{
					var source  = (SqlTableLikeSource)Visit(element.Source);
					var inserts = VisitElements(element.Inserts, VisitMode.Transform);
					var ext     = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)                   ||
						!ReferenceEquals(element.Source, source) ||
					    element.Inserts            != inserts    ||
					    element.SqlQueryExtensions != ext)
					{
						return NotifyReplaced(new SqlMultiInsertStatement(
								element.InsertType,
								source,
								element.Inserts != inserts ? inserts : inserts.ToList())
							{
								SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
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

		protected virtual IQueryElement VisitSqlMergeStatement(SqlMergeStatement element)
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
					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag    = (SqlComment?)Visit(element.Tag);
					element.With   = (SqlWithClause?)Visit(element.With);
					var target     = (SqlTableSource)Visit(element.Target);
					var source     = (SqlTableLikeSource)Visit(element.Source);
					var on         = (SqlSearchCondition)Visit(element.On);
					var output     = (SqlOutputClause?)Visit(element.Output);

					VisitElements(element.Operations, VisitMode.Modify);
					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					element.Modify(target, source, on, output);

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
					var ext        = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)                   ||
					    !ReferenceEquals(element.Tag, tag)       ||
					    !ReferenceEquals(element.With, with)     ||
					    !ReferenceEquals(element.Target, target) ||
					    !ReferenceEquals(element.Source, source) ||
					    !ReferenceEquals(element.On, on)         ||
					    !ReferenceEquals(element.Output, output) ||
					    element.Operations         != operations ||
					    element.SqlQueryExtensions != ext
					   )
					{
						return NotifyReplaced(
							new SqlMergeStatement(
								with,
								element.Hint,
								target,
								source,
								on,
								element.Operations != operations ? operations : operations.ToList())
							{
								Tag                = tag,
								Output             = output,
								SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
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

		protected virtual IQueryElement VisitSqlDeleteStatement(SqlDeleteStatement element)
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

					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

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

					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

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
					var ext         = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)                             ||
					    !ReferenceEquals(element.SelectQuery, selectQuery) ||
					    !ReferenceEquals(element.Tag, tag)                 ||
					    !ReferenceEquals(element.With, with)               ||
					    !ReferenceEquals(element.Table, table)             ||
					    !ReferenceEquals(element.Top, top)                 ||
					    !ReferenceEquals(element.Output, output)           ||
					    element.SqlQueryExtensions != ext
					   )
					{
						return NotifyReplaced(
							new SqlDeleteStatement(selectQuery)
							{
								Tag                = tag,
								With               = with,
								Table              = table,
								Top                = top,
								Output             = output,
								SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList(),
							}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlUpdateStatement(SqlUpdateStatement element)
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

					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag         = (SqlComment?)Visit(element.Tag);
					element.With        = (SqlWithClause?)Visit(element.With);
					element.SelectQuery = (SelectQuery?)Visit(element.SelectQuery);
					element.Update      = (SqlUpdateClause)Visit(element.Update);
					element.Output      = (SqlOutputClause?)Visit(element.Output);

					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var tag         = (SqlComment?)Visit(element.Tag);
					var with        = (SqlWithClause?)Visit(element.With);
					var selectQuery = (SelectQuery?)Visit(element.SelectQuery);
					var update      = (SqlUpdateClause)Visit(element.Update);
					var output      = (SqlOutputClause?)Visit(element.Output);
					var ext         = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)                             ||
					    !ReferenceEquals(element.SelectQuery, selectQuery) ||
					    !ReferenceEquals(element.Tag, tag)                 ||
					    !ReferenceEquals(element.With, with)               ||
					    !ReferenceEquals(element.Update, update)           ||
					    !ReferenceEquals(element.Output, output)           ||
					    element.SqlQueryExtensions != ext
					   )
					{
						return NotifyReplaced(
							new SqlUpdateStatement(selectQuery)
							{
								Tag                = tag,
								With               = with,
								Update             = update,
								Output             = output,
								SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
							}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlInsertOrUpdateStatement(SqlInsertOrUpdateStatement element)
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

					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag         = (SqlComment?)Visit(element.Tag);
					element.With        = (SqlWithClause?)Visit(element.With);
					element.SelectQuery = (SelectQuery?)Visit(element.SelectQuery);
					element.Insert      = (SqlInsertClause)Visit(element.Insert);
					element.Update      = (SqlUpdateClause)Visit(element.Update);

					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var tag         = (SqlComment?)Visit(element.Tag);
					var with        = (SqlWithClause?)Visit(element.With);
					var selectQuery = (SelectQuery?)Visit(element.SelectQuery);
					var insert      = (SqlInsertClause)Visit(element.Insert);
					var update      = (SqlUpdateClause)Visit(element.Update);
					var ext         = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)                             ||
					    !ReferenceEquals(element.SelectQuery, selectQuery) ||
					    !ReferenceEquals(element.Tag, tag)                 ||
					    !ReferenceEquals(element.With, with)               ||
					    !ReferenceEquals(element.Insert, insert)           ||
					    !ReferenceEquals(element.Update, update)           ||
					    element.SqlQueryExtensions != ext)
					{
						return NotifyReplaced(new SqlInsertOrUpdateStatement(selectQuery)
						{
							Tag                = tag,
							With               = with,
							Insert             = insert,
							Update             = update,
							SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
						}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlInsertStatement(SqlInsertStatement element)
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

					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag         = (SqlComment?)Visit(element.Tag);
					element.With        = (SqlWithClause?)Visit(element.With);
					element.SelectQuery = (SelectQuery?)Visit(element.SelectQuery);
					element.Insert      = (SqlInsertClause)Visit(element.Insert);
					element.Output      = (SqlOutputClause?)Visit(element.Output);

					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var tag         = (SqlComment?)Visit(element.Tag);
					var with        = (SqlWithClause?)Visit(element.With);
					var selectQuery = (SelectQuery?)Visit(element.SelectQuery);
					var insert      = (SqlInsertClause)Visit(element.Insert);
					var output      = (SqlOutputClause?)Visit(element.Output);
					var ext         = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)                             ||
					    !ReferenceEquals(element.SelectQuery, selectQuery) ||
					    !ReferenceEquals(element.Tag, tag)                 ||
					    !ReferenceEquals(element.With, with)               ||
					    !ReferenceEquals(element.Insert, insert)           ||
					    !ReferenceEquals(element.Output, output)           ||
					    element.SqlQueryExtensions != ext)
					{
						return NotifyReplaced(new SqlInsertStatement(selectQuery)
						{
							Tag                = tag,
							With               = with,
							Insert             = insert,
							Output             = output,
							SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
						}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlSelectStatement(SqlSelectStatement element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Tag);
					Visit(element.With);
					Visit(element.SelectQuery);

					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					element.Tag         = (SqlComment?)Visit(element.Tag);
					element.With        = (SqlWithClause?)Visit(element.With);
					element.SelectQuery = (SelectQuery?)Visit(element.SelectQuery);

					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var tag         = (SqlComment?)Visit(element.Tag);
					var with        = (SqlWithClause?)Visit(element.With);
					var selectQuery = (SelectQuery?)Visit(element.SelectQuery);
					var ext         = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)                             ||
					    !ReferenceEquals(element.SelectQuery, selectQuery) ||
					    !ReferenceEquals(element.Tag, tag)                 ||
					    !ReferenceEquals(element.With, with)               ||
					    element.SqlQueryExtensions != ext)
					{
						return NotifyReplaced(new SqlSelectStatement(selectQuery)
						{
							Tag                = tag,
							With               = with,
							SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
						}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlOutputClause(SqlOutputClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.OutputTable);

					VisitElements(element.OutputColumns, VisitMode.ReadOnly);

					if (element.HasOutputItems)
					{
						VisitElements(element.OutputItems, VisitMode.ReadOnly);
					}

					break;
				}
				case VisitMode.Modify:
				{
					var outputTable   = (SqlTable?)Visit(element.OutputTable);

					VisitElements(element.OutputColumns, VisitMode.Modify);

					if (element.HasOutputItems)
					{
						VisitElements(element.OutputItems, VisitMode.Modify);
					}

					element.Modify(outputTable);

					break;
				}
				case VisitMode.Transform:
				{
					var outputTable   = (SqlTable?)Visit(element.OutputTable);
					var outputColumns = VisitElements(element.OutputColumns, VisitMode.Transform);
					var outputItems   = element.HasOutputItems ? VisitElements(element.OutputItems, VisitMode.Transform) : null;

					if (ShouldReplace(element)                                 ||
					    !ReferenceEquals(element.OutputTable, outputTable)     ||
					    element.OutputColumns != outputColumns                 ||
					    (element.HasOutputItems && element.OutputItems != outputItems)
					   )
					{
						var newElement = NotifyReplaced(new SqlOutputClause()
						{
							OutputTable   = outputTable,
							OutputColumns = element.OutputColumns != outputColumns ? outputColumns : outputColumns?.ToList(),
							OutputItems   = element.HasOutputItems ? (element.OutputItems != outputItems!
								? outputItems!
								: outputItems!.ToList()) : null! // TODO: refactor HasOutputItems/OutputItems...
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

		protected virtual IQueryElement VisitSqlValuesTable(SqlValuesTable element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitListOfLists(element.Rows, VisitMode.ReadOnly);

					Visit(element.Source);

					break;
				}
				case VisitMode.Modify:
				{
					VisitListOfLists(element.Rows, VisitMode.Modify);
					element.Modify(Visit(element.Source) as ISqlExpression);

					break;
				}
				case VisitMode.Transform:
				{
					var rows   = VisitListOfLists(element.Rows, VisitMode.Transform);
					var source = Visit(element.Source) as ISqlExpression;

					if (ShouldReplace(element) || rows != element.Rows || !ReferenceEquals(source, element.Source))
					{
						var newFields = CopyFields(element.Fields);

						var sqlValuesTable = new SqlValuesTable(source, element.ValueBuilders, newFields, rows);
						if (element.FieldsLookup != null)
						{
							sqlValuesTable.FieldsLookup =
								element.FieldsLookup.ToDictionary(e => e.Key, e => (SqlField)Visit(e.Value));
						}

						return NotifyReplaced(sqlValuesTable, element);
					}

					break;

				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
			
		}

		protected virtual IQueryElement VisitSqlRawSqlTable(SqlRawSqlTable element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Parameters, VisitMode.ReadOnly);
					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					VisitElements(element.Parameters, VisitMode.Modify);
					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var parameters = VisitElements(element.Parameters, VisitMode.Transform);
					var ext        = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)           ||
					    element.Parameters != parameters ||
					    element.SqlQueryExtensions != ext)
					{
						var newTable = new SqlRawSqlTable(element, element.Parameters != parameters ? parameters : parameters.ToArray())
						{
							SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
						};

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

		protected virtual IQueryElement VisitSqlCteTable(SqlCteTable element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					if (element.Cte != null)
						VisitCteClauseReference(element.Cte);

					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					if (element.Cte != null)
						element.Cte = (CteClause)VisitCteClauseReference(element.Cte);

					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var clause = element.Cte != null ? (CteClause)VisitCteClauseReference(element.Cte) : null;

					var ext = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element) ||
						element.Cte != clause  ||
						element.SqlQueryExtensions != ext)
					{
						var newFields = CopyFields(element.Fields);
						var newTable = new SqlCteTable(element, newFields, clause)
						{
							SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
						};

						return NotifyReplaced(newTable, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlWithClause(SqlWithClause element)
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
					VisitElements(element.Clauses, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var clauses = VisitElements(element.Clauses, VisitMode.Transform);
					if (ShouldReplace(element) || element.Clauses != clauses)
					{
						return NotifyReplaced(
							new SqlWithClause { Clauses = element.Clauses != clauses ? clauses : clauses.ToList() },
							element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlSetOperator(SqlSetOperator element)
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

		protected virtual IQueryElement VisitSqlOrderByItem(SqlOrderByItem element)
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
						return NotifyReplaced(new SqlOrderByItem(e, element.IsDescending, element.IsPositioned), element);

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlOrderByClause(SqlOrderByClause element)
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

					if (ShouldReplace(element) || element.Items != items)
					{
						return NotifyReplaced(new SqlOrderByClause(element.Items != items ? items : items.ToList()), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual ISqlExpression VisitSqlGroupByItem(ISqlExpression element)
		{
			return (ISqlExpression) Visit(element);
		}

		protected virtual IQueryElement VisitSqlGroupByClause(SqlGroupByClause element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Items, VisitMode.ReadOnly, VisitSqlGroupByItem);

					break;
				}
				case VisitMode.Modify:
				{
					VisitElements(element.Items, VisitMode.Modify, VisitSqlGroupByItem);

					break;
				}
				case VisitMode.Transform:
				{
					var items = VisitElements(element.Items, VisitMode.Transform, VisitSqlGroupByItem);

					if (ShouldReplace(element) || element.Items != items)
					{
						return NotifyReplaced(new SqlGroupByClause(element.GroupingType, element.Items != items ? items : items.ToList()), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlWhereClause(SqlWhereClause element)
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

		protected virtual IQueryElement VisitSqlHavingClause(SqlHavingClause element)
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
						return NotifyReplaced(new SqlHavingClause(searchCond), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlFromClause(SqlFromClause element)
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

					if (ShouldReplace(element) || element.Tables != tables)
					{
						return NotifyReplaced(new SqlFromClause(element.Tables != tables ? tables : tables.ToList()), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlSetExpression(SqlSetExpression element)
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

		protected virtual IQueryElement VisitSqlUpdateClause(SqlUpdateClause element)
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

					VisitElements(element.Items, VisitMode.Modify);
					VisitElements(element.Keys, VisitMode.Modify);

					element.Modify(table, ts);

					break;
				}
				case VisitMode.Transform:
				{
					var table = (SqlTable?)Visit(element.Table);
					var ts    = (SqlTableSource?)Visit(element.TableSource);
					var items = VisitElements(element.Items, VisitMode.Transform);
					var keys  = VisitElements(element.Keys, VisitMode.Transform);

					if (ShouldReplace(element)                    ||
					    !ReferenceEquals(element.Table, table)    ||
					    !ReferenceEquals(element.TableSource, ts) ||
					    element.Items != items                    ||
					    element.Keys != keys)
					{
						var newUpdate = new SqlUpdateClause()
						{
							Table       = table,
							TableSource = ts,
							Items       = element.Items != items ? items : items.ToList(),
							Keys        = element.Keys != keys ? keys : keys.ToList(),
						};

						return NotifyReplaced(newUpdate, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlInsertClause(SqlInsertClause element)
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

					element.Modify(into);

					break;
				}
				case VisitMode.Transform:
				{
					var into  = (SqlTable?)Visit(element.Into);
					var items = VisitElements(element.Items, VisitMode.Transform);

					if (ShouldReplace(element)               ||
					    !ReferenceEquals(element.Into, into) ||
					    element.Items != items)
					{
						return NotifyReplaced(
							new SqlInsertClause
							{
								Into         = into,
								Items        = element.Items != items ? items : items.ToList(),
								WithIdentity = element.WithIdentity
							}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Table);
					Visit(element.Condition);
					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					element.Table     = (SqlTableSource)Visit(element.Table);
					element.Condition = (SqlSearchCondition)Visit(element.Condition);
					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var table = (SqlTableSource)Visit(element.Table);
					var cond  = (SqlSearchCondition)Visit(element.Condition);
					var ext   = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)                    ||
					    !ReferenceEquals(table, element.Table)    ||
					    !ReferenceEquals(cond, element.Condition) ||
					    element.SqlQueryExtensions != ext)
					{
						return NotifyReplaced(
							new SqlJoinedTable(element.JoinType, table, element.IsWeak, cond)
							{
								SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
							}, element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlTableSource(SqlTableSource element)
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
					VisitElements(element.Joins, VisitMode.Modify);

					if (element.HasUniqueKeys)
						VisitListOfArrays(element.UniqueKeys, VisitMode.Modify);

					element.Modify(source);

					break;
				}
				case VisitMode.Transform:
				{
					var source = (ISqlTableSource)Visit(element.Source);
					var joins  = VisitElements(element.Joins, VisitMode.Transform);

					var uk = element.HasUniqueKeys ? VisitListOfArrays(element.UniqueKeys, VisitMode.Transform) : null;

					if (ShouldReplace(element)                              ||
					    !ReferenceEquals(source, element.Source)            ||
						(element.HasUniqueKeys && element.UniqueKeys != uk) ||
					    element.Joins != joins)
					{
						return NotifyReplaced(new SqlTableSource(source, element.RawAlias, element.Joins != joins ? joins : joins.ToList(), uk), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlSearchCondition(SqlSearchCondition element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Predicates, VisitMode.ReadOnly);
					break;
				}
				case VisitMode.Modify:
				{
					VisitElements(element.Predicates, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var predicates = VisitElements(element.Predicates, VisitMode.Transform);

					if (ShouldReplace(element) || element.Predicates != predicates)
					{
						return NotifyReplaced(new SqlSearchCondition(element.IsOr, element.Predicates != predicates ? predicates : predicates.ToList()), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlSelectClause(SqlSelectClause element)
		{
			// note that for column definitions we don't want to call by-ref visitor (VisitSqlColumnReference)
			// column visit implementation similar to table fields visit (CopyFields)
			// with one distinction - as column stores Expression, we must visit it first
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

					ISqlExpression?[]? newExpressions = null;

					for (var i = 0; i < element.Columns.Count; i++)
					{
						var column = element.Columns[i];
						var expr   = VisitSqlColumnExpression(column, column.Expression);

						if (!ReferenceEquals(expr, column.Expression))
							(newExpressions ??= new ISqlExpression?[element.Columns.Count])[i] = expr;
					}

					if (ShouldReplace(element)                    ||
						newExpressions != null                    ||
						!ReferenceEquals(element.TakeValue, take) ||
						!ReferenceEquals(element.SkipValue, skip))
					{
						// we always clone columns due to SqlColumn.Parent reference, also see VisitSqlQuery notes for details
						var newColumns = new SqlColumn[element.Columns.Count];
						for (var i = 0; i < element.Columns.Count; i++)
						{
							var oldColumn = element.Columns[i];
							var newColumn = newColumns[i] = new SqlColumn(element.SelectQuery, newExpressions?[i] ?? oldColumn.Expression, oldColumn.RawAlias);
							NotifyReplaced(newColumn, oldColumn);
						}

						return NotifyReplaced(new SqlSelectClause(element.IsDistinct, take, element.TakeHints, skip, newColumns), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlQuery(SelectQuery selectQuery)
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
						VisitElements(selectQuery.SetOperators, VisitMode.ReadOnly);

					if (selectQuery.HasUniqueKeys)
						VisitListOfArrays(selectQuery.UniqueKeys, VisitMode.ReadOnly);

					VisitElements(selectQuery.SqlQueryExtensions, VisitMode.ReadOnly);

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
						VisitElements(selectQuery.SetOperators, VisitMode.Modify);

					if (selectQuery.HasUniqueKeys)
						VisitListOfArrays(selectQuery.UniqueKeys, VisitMode.Modify);

					VisitElements(selectQuery.SqlQueryExtensions, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var fc = (SqlFromClause)   Visit(selectQuery.From   );
					var sc = (SqlSelectClause) Visit(selectQuery.Select );
					var wc = (SqlWhereClause)  Visit(selectQuery.Where  );
					var gc = (SqlGroupByClause)Visit(selectQuery.GroupBy);
					var hc = (SqlHavingClause) Visit(selectQuery.Having );
					var oc = (SqlOrderByClause)Visit(selectQuery.OrderBy);

					var so = selectQuery.HasSetOperators ? VisitElements    (selectQuery.SetOperators, VisitMode.Transform) : null;
					var uk = selectQuery.HasUniqueKeys   ? VisitListOfArrays(selectQuery.UniqueKeys  , VisitMode.Transform) : null;

					var ex = VisitElements(selectQuery.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(selectQuery)
						|| !ReferenceEquals(fc, selectQuery.From)
						|| !ReferenceEquals(sc, selectQuery.Select)
						|| !ReferenceEquals(wc, selectQuery.Where)
						|| !ReferenceEquals(gc, selectQuery.GroupBy)
						|| !ReferenceEquals(hc, selectQuery.Having)
						|| !ReferenceEquals(oc, selectQuery.OrderBy)
						|| (selectQuery.HasSetOperators && so != selectQuery.SetOperators)
						|| (selectQuery.HasUniqueKeys   && uk != selectQuery.UniqueKeys)
						|| selectQuery.SqlQueryExtensions != ex)
					{
						// we force clone strong components (clauses) of select query, that were not cloned above
						// as they cannot belong to more than one query due to Parent reference to SelectQuery instance
						// removal of such reference is not an easy task currently
						//
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

							for (int i = 0; i < selectQuery.Select.Columns.Count; i++)
							{
								var oldColumn = selectQuery.Select.Columns[i];
								var newColumn = sc.Columns[i];
								NotifyReplaced(newColumn, oldColumn);
							}

							NotifyReplaced(sc, selectQuery.Select);
						}
						else
						{
							// all columns already copied by VisitSqlSelectClause, just reassign query
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
							hc                 = new SqlHavingClause(nq);
							hc.SearchCondition = selectQuery.Having.SearchCondition;

							NotifyReplaced(hc, selectQuery.Having);
						}

						if (ReferenceEquals(oc, selectQuery.OrderBy))
						{
							oc = new SqlOrderByClause(nq);
							oc.Items.AddRange(selectQuery.OrderBy.Items);

							NotifyReplaced(oc, selectQuery.OrderBy);
						}

						if (selectQuery.HasSetOperators)
						{
							if (so == selectQuery.SetOperators)
								so = so.ToList();
						}

						if (selectQuery.HasUniqueKeys)
						{
							if (uk == selectQuery.UniqueKeys)
								uk = uk.ToList();
						}

						if (selectQuery.SqlQueryExtensions == ex)
							ex = ex?.ToList();

						nq.Init(sc, fc, wc, gc, hc, oc, so, uk,
							selectQuery.IsParameterDependent,
							selectQuery.QueryName,
							selectQuery.DoNotSetAliases);

						nq.SqlQueryExtensions = ex;

						return NotifyReplaced(nq, selectQuery);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return selectQuery;
		}

		protected virtual IQueryElement VisitExistsPredicate(SqlPredicate.Exists predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
				{
					Visit(predicate.SubQuery);
					break;
				}
				case VisitMode.Modify:
				{
					var subQuery = (SelectQuery)Visit(predicate.SubQuery);

					predicate.Modify(subQuery);

					break;
				}
				case VisitMode.Transform:
				{
					var subQuery = (SelectQuery)Visit(predicate.SubQuery);

					if (ShouldReplace(predicate) ||
						!ReferenceEquals(predicate.SubQuery, subQuery))
					{
						return NotifyReplaced(new SqlPredicate.Exists(predicate.IsNot, subQuery), predicate);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		protected virtual IQueryElement VisitInListPredicate(SqlPredicate.InList predicate)
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
					VisitElements(predicate.Values, VisitMode.Modify);

					predicate.Modify(expr1);

					break;
				}
				case VisitMode.Transform:
				{
					var expr1  = (ISqlExpression)Visit(predicate.Expr1);
					var values = VisitElements(predicate.Values, VisitMode.Transform);

					if (ShouldReplace(predicate)                 ||
					    !ReferenceEquals(predicate.Expr1, expr1) ||
					    predicate.Values != values)
					{
						return NotifyReplaced(
							new SqlPredicate.InList(expr1, predicate.WithNull, predicate.IsNot,
								predicate.Values != values ? values : values.ToList()), predicate);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		protected virtual IQueryElement VisitInSubQueryPredicate(SqlPredicate.InSubQuery predicate)
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
						return NotifyReplaced(new SqlPredicate.InSubQuery(expr1, predicate.IsNot, subQuery, predicate.DoNotConvert), predicate);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		protected virtual IQueryElement VisitIsTruePredicate(SqlPredicate.IsTrue predicate)
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

		protected virtual IQueryElement VisitIsDistinctPredicate(SqlPredicate.IsDistinct predicate)
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

					if (ShouldReplace(predicate)              ||
						!ReferenceEquals(predicate.Expr1, e1) ||
						!ReferenceEquals(predicate.Expr2, e2))
						return NotifyReplaced(new SqlPredicate.IsDistinct(e1, predicate.IsNot, e2), predicate);

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		protected virtual IQueryElement VisitIsNullPredicate(SqlPredicate.IsNull predicate)
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

		protected virtual IQueryElement VisitBetweenPredicate(SqlPredicate.Between predicate)
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

					if (ShouldReplace(predicate)                 ||
						!ReferenceEquals(predicate.Expr1, expr1) ||
						!ReferenceEquals(predicate.Expr2, expr2) ||
						!ReferenceEquals(predicate.Expr3, expr3))
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

		protected virtual IQueryElement VisitSearchStringPredicate(SqlPredicate.SearchString predicate)
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
					var expr1         = (ISqlExpression)Visit(predicate.Expr1);
					var expr2         = (ISqlExpression)Visit(predicate.Expr2);
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

		protected virtual IQueryElement VisitLikePredicate(SqlPredicate.Like predicate)
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

					if (ShouldReplace(predicate)              ||
						!ReferenceEquals(predicate.Expr1, e1) ||
					    !ReferenceEquals(predicate.Expr2, e2) ||
						!ReferenceEquals(predicate.Escape, esc))
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

		protected virtual IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
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

		protected virtual IQueryElement VisitExprPredicate(SqlPredicate.Expr predicate)
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

		protected virtual IQueryElement VisitSqlRow(SqlRowExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Values, VisitMode.ReadOnly);
					break;
				}
				case VisitMode.Modify:
				{
					VisitElements(element.Values, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var values = VisitElements(element.Values, VisitMode.Transform);

					if (ShouldReplace(element) || element.Values != values)
					{
						return NotifyReplaced(new SqlRowExpression(element.Values != values ? values : values.ToArray()), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitNotPredicate(SqlPredicate.Not predicate)
		{
			switch (GetVisitMode(predicate))
			{
				case VisitMode.ReadOnly:
					Visit(predicate.Predicate);
					break;
				case VisitMode.Modify:
					predicate.Modify((ISqlPredicate)Visit(predicate.Predicate));
					break;
				case VisitMode.Transform:
				{
					var p = (ISqlPredicate)Visit(predicate.Predicate);

					if (ShouldReplace(predicate) || !ReferenceEquals(predicate.Predicate, p))
					{
						return NotifyReplaced(new SqlPredicate.Not(p), predicate);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return predicate;
		}

		protected virtual IQueryElement VisitTruePredicate(SqlPredicate.TruePredicate predicate) => predicate;

		protected virtual IQueryElement VisitFalsePredicate(SqlPredicate.FalsePredicate predicate) => predicate;

		protected virtual IQueryElement VisitSqlAliasPlaceholder(SqlAliasPlaceholder element) => element;

		protected virtual IQueryElement VisitSqlTable(SqlTable element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.TableArguments, VisitMode.ReadOnly);
					VisitElements(element.SqlQueryExtensions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					VisitElements(element.TableArguments, VisitMode.Modify);
					VisitElements(element.SqlQueryExtensions, VisitMode.Modify);

					break;
				}
				case VisitMode.Transform:
				{
					var tableArguments = VisitElements(element.TableArguments, VisitMode.Transform);
					var ext            = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

					if (ShouldReplace(element)                   ||
					    element.TableArguments != tableArguments ||
					    element.SqlQueryExtensions != ext)
					{
						var newTable = new SqlTable(element)
						{
							TableArguments     = element.TableArguments != tableArguments ? tableArguments : tableArguments?.ToArray(),
							SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
						};

						NotifyReplaced(newTable.All, element.All);

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

		protected virtual IQueryElement VisitSqlDataType(SqlDataType element) => element;

		protected virtual IQueryElement VisitSqlValue(SqlValue element) => element;

		protected virtual IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Expr1);
					Visit(element.Expr2);
					break;
				}
				case VisitMode.Modify:
				{
					element.Expr1 = (ISqlExpression)Visit(element.Expr1);
					element.Expr2 = (ISqlExpression)Visit(element.Expr2);
					break;
				}
				case VisitMode.Transform:
				{
					var expr1 = (ISqlExpression)Visit(element.Expr1);
					var expr2 = (ISqlExpression)Visit(element.Expr2);

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

		protected virtual IQueryElement VisitSqlObjectExpression(SqlObjectExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					// TODO: probably we should convert SqlGetValue to ISqlElement to avoid manual visit
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

		protected virtual IQueryElement VisitSqlAnchor(SqlAnchor element)
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

		protected virtual IQueryElement VisitSqlNullabilityExpression(SqlNullabilityExpression element)
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

					if (ShouldReplace(element) || !ReferenceEquals(sqlExpr, element.SqlExpression))
						return NotifyReplaced(new SqlNullabilityExpression(sqlExpr, element.CanBeNull), element);

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlExpression(SqlExpression element)
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

					if (ShouldReplace(element) || parameters != element.Parameters)
					{
						return NotifyReplaced(new SqlExpression(
							element.SystemType, element.Expr, element.Precedence,
							element.Flags, element.NullabilityType, element.CanBeNullNullable, parameters != element.Parameters ? parameters : parameters.ToArray()),
							element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlParameter(SqlParameter sqlParameter) => sqlParameter;

		protected virtual IQueryElement VisitSqlFunction(SqlFunction element)
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

					if (ShouldReplace(element) || parameters != element.Parameters)
					{
						return NotifyReplaced(
							new SqlFunction(element.SystemType, element.Name, element.IsAggregate,
								element.IsPure,
								element.Precedence, element.NullabilityType, element.CanBeNullNullable, parameters != element.Parameters ? parameters : parameters.ToArray())
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

		protected virtual IQueryElement VisitSqlQueryExtension(SqlQueryExtension extension)
		{
			switch (GetVisitMode(extension))
			{
				case VisitMode.ReadOnly:
				{
					foreach(var a in extension.Arguments.Values)
						Visit(a);
					break;
				}
				case VisitMode.Modify:
				{
					var current  = extension.Arguments;

					Dictionary<string, ISqlExpression>? modified = null;
					foreach(var pair in current)
					{
						var newValue = (ISqlExpression)Visit(pair.Value);
						if (!ReferenceEquals(newValue, pair.Value))
						{
							(modified ??= new ()).Add(pair.Key, newValue);
						}
					};

					if (modified != null)
					{
						foreach(var m in modified)
						{
							current[m.Key] = m.Value;
						}
					}

					break;
				}
				case VisitMode.Transform:
				{
					var current = extension.Arguments;
					Dictionary<string, ISqlExpression>? modified = null;

					foreach (var pair in current)
					{
						var newValue = (ISqlExpression)Visit(pair.Value);
						if (!ReferenceEquals(newValue, pair.Value))
						{
							(modified ??= new()).Add(pair.Key, newValue);
						}
					};

					if (modified != null)
					{
						foreach(var m in current)
						{
#if NET6_0_OR_GREATER
							modified.TryAdd(m.Key, m.Value);
#else
							if (!modified.ContainsKey(m.Key))
								modified.Add(m.Key, m.Value);
#endif
						}

						current = modified;
					}

					if (ShouldReplace(extension) || current != extension.Arguments)
					{
						var newExtension = new SqlQueryExtension()
						{
							Arguments     = current != extension.Arguments ? current : new(extension.Arguments),
							BuilderType   = extension.BuilderType,
							Configuration = extension.Configuration,
							Scope         = extension.Scope
						};

						return NotifyReplaced(newExtension, extension);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return extension;
		}

		protected virtual IQueryElement VisitSqlConditionExpression(SqlConditionExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Condition);
					Visit(element.TrueValue);
					Visit(element.FalseValue);

					break;
				}
				case VisitMode.Modify:
				{
					element.Modify(
						(ISqlPredicate)Visit(element.Condition),
						(ISqlExpression)Visit(element.TrueValue),
						(ISqlExpression)Visit(element.FalseValue)
					);

					break;
				}
				case VisitMode.Transform:
				{
					var predicate  = (ISqlPredicate)Visit(element.Condition);
					var trueValue  = (ISqlExpression)Visit(element.TrueValue);
					var falseValue = (ISqlExpression)Visit(element.FalseValue);

					if (ShouldReplace(element)                         || 
					    element.Condition != predicate                 || 
					    !ReferenceEquals(element.TrueValue, trueValue) || 
					    !ReferenceEquals(element.FalseValue, falseValue))
					{
						return NotifyReplaced(new SqlConditionExpression(predicate, trueValue, falseValue), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlCastExpression(SqlCastExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Expression);
					Visit(Visit(element.FromType));
					break;
				}
				case VisitMode.Modify:
				{
					element.Modify(element.ToType,  (ISqlExpression)Visit(element.Expression), (SqlDataType?)Visit(element.FromType));
					break;
				}
				case VisitMode.Transform:
				{
					var expression = (ISqlExpression)Visit(element.Expression);
					var fromType   = (SqlDataType?)Visit(element.FromType);

					if (ShouldReplace(element) || !ReferenceEquals(element.Expression, expression) || !ReferenceEquals(element.FromType, fromType))
					{
						return NotifyReplaced(new SqlCastExpression(expression, element.ToType, fromType), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlCoalesceExpression(SqlCoalesceExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element.Expressions, VisitMode.ReadOnly);

					break;
				}
				case VisitMode.Modify:
				{
					element.Modify(VisitElements(element.Expressions, VisitMode.Modify));

					break;
				}
				case VisitMode.Transform:
				{
					var expressions = VisitElements(element.Expressions, VisitMode.Transform);

					if (ShouldReplace(element)                             || 
					    !ReferenceEquals(element.Expressions, expressions))
					{
						return NotifyReplaced(new SqlCoalesceExpression(element.Expressions != expressions ? expressions : expressions.ToArray()), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual SqlCaseExpression.CaseItem VisitCaseItem(SqlCaseExpression.CaseItem element)
		{
			return element.Update((ISqlPredicate)Visit(element.Condition), (ISqlExpression)Visit(element.ResultExpression));
		}

		protected virtual IQueryElement VisitSqlCaseExpression(SqlCaseExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					VisitElements(element._cases, VisitMode.ReadOnly, VisitCaseItem);
					Visit(element.ElseExpression);

					break;
				}
				case VisitMode.Modify:
				{
					var newElements = VisitElements(element._cases, VisitMode.Modify, VisitCaseItem);
					var defaultExpr = (ISqlExpression?)Visit(element.ElseExpression);

					element.Modify(newElements, defaultExpr);

					break;
				}
				case VisitMode.Transform:
				{
					var newElements = VisitElements(element._cases, VisitMode.Transform, VisitCaseItem);
					var elseExpr = (ISqlExpression?)Visit(element.ElseExpression);

					if (ShouldReplace(element)                        || 
					    !ReferenceEquals(element._cases, newElements) ||
					    !ReferenceEquals(element.ElseExpression, elseExpr))
					{
						return NotifyReplaced(new SqlCaseExpression(element.Type, element._cases != newElements ? newElements : element._cases.ToList(), elseExpr), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		protected virtual IQueryElement VisitSqlCompareToExpression(SqlCompareToExpression element)
		{
			switch (GetVisitMode(element))
			{
				case VisitMode.ReadOnly:
				{
					Visit(element.Expression1);
					Visit(element.Expression2);

					break;
				}
				case VisitMode.Modify:
				{
					element.Modify((ISqlExpression)Visit(element.Expression1), (ISqlExpression)Visit(element.Expression2));

					break;
				}
				case VisitMode.Transform:
				{
					var expression1 = (ISqlExpression)Visit(element.Expression1);
					var expression2 = (ISqlExpression)Visit(element.Expression2);

					if (ShouldReplace(element)                             || 
					    !ReferenceEquals(element.Expression1, expression1) ||
					    !ReferenceEquals(element.Expression2, expression2))
					{
						return NotifyReplaced(new SqlCompareToExpression(expression1, expression2), element);
					}

					break;
				}
				default:
					throw CreateInvalidVisitModeException();
			}

			return element;
		}

		#endregion

		#region Helper functions

		/// <summary>
		/// Creates copy of <see cref="SqlField"/> without table set and call <see cref="NotifyReplaced(IQueryElement, IQueryElement)"/> for each.
		/// </summary>
		protected IReadOnlyList<SqlField> CopyFields(IReadOnlyList<SqlField> fields)
		{
			var newFields = new SqlField[fields.Count];
			for (var i = 0; i < fields.Count; i++)
			{
				var oldField = fields[i];
				var newField = newFields[i] = new SqlField(oldField);
				NotifyReplaced(newField, oldField);
			}

			return newFields;
		}

		protected Exception CreateInvalidVisitModeException([CallerMemberName] string? methodName = null)
			=> new InvalidOperationException($"Invalid VisitMode in '{methodName}'");

		/// <summary>
		/// Visits array of query elements.
		/// </summary>
		/// <returns>
		/// Return value depends on <paramref name="mode"/> value:
		/// <list type="bullet">
		/// <item><c>null</c> when <paramref name="arr1"/> is <c>null</c>;</item>
		/// <item><see cref="VisitMode.ReadOnly"/>: returns input array <paramref name="arr1"/> instance;</item>
		/// <item><see cref="VisitMode.Modify"/>: returns input array <paramref name="arr1"/> instance, could contain inplace array element replacements;</item>
		/// <item><see cref="VisitMode.Transform"/>: returns new array instance when there were changes to array items; otherwise returns original array.</item>
		/// </list>
		/// </returns>
		[return: NotNullIfNotNull(nameof(arr1))]
		protected T[]? VisitElements<T>(T[]? arr1, VisitMode mode)
			where T : class, IQueryElement
		{
			if (arr1 == null)
				return null;

			switch (mode)
			{
				case VisitMode.ReadOnly:
				{
					foreach (var t in arr1)
					{
						_ = Visit(t);
					}

					return arr1;
				}

				case VisitMode.Modify:
				{
					for (var i = 0; i < arr1.Length; i++)
					{
						arr1[i] = (T)Visit(arr1[i]);
					}

					return arr1;
				}

				case VisitMode.Transform:
				{
					T[]? arr2 = null;

					for (var i = 0; i < arr1.Length; i++)
					{
						var elem1 = arr1[i];
						var elem2 = (T)Visit(elem1);

						if (!ReferenceEquals(elem1, elem2))
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

					return arr2 ?? arr1;
				}
				default:
					throw CreateInvalidVisitModeException();
			}
		}

		/// <summary>
		/// Visits list of query elements.
		/// </summary>
		/// <returns>
		/// Return value depends on <paramref name="mode"/> value:
		/// <list type="bullet">
		/// <item><c>null</c> when <paramref name="list1"/> is <c>null</c>;</item>
		/// <item><see cref="VisitMode.ReadOnly"/>: returns input list <paramref name="list1"/> instance;</item>
		/// <item><see cref="VisitMode.Modify"/>: returns input list <paramref name="list1"/> instance, could contain inplace list item replacements;</item>
		/// <item><see cref="VisitMode.Transform"/>: returns new list instance when there were changes to list items; otherwise returns original list.</item>
		/// </list>
		/// </returns>
		[return: NotNullIfNotNull(nameof(list1))]
		protected List<T>? VisitElements<T>(List<T>? list1, VisitMode mode)
			where T : class, IQueryElement
		{
			if (list1 == null)
				return null;

			switch (mode)
			{
				case VisitMode.ReadOnly:
				{
					foreach (var t in list1)
					{
						_ = Visit(t);
					}

					return list1;
				}

				case VisitMode.Modify:
				{
					for (var i = 0; i < list1.Count; i++)
					{
						list1[i] = (T)Visit(list1[i]);
					}

					return list1;
				}

				case VisitMode.Transform:
				{
					List<T>? list2 = null;

					for (var i = 0; i < list1.Count; i++)
					{
						var elem1 = list1[i];
						var elem2 = (T)Visit(elem1);

						if (!ReferenceEquals(elem1, elem2))
						{
							if (list2 == null)
							{
								list2 = new List<T>(list1.Count);

								for (var j = 0; j < i; j++)
									list2.Add(list1[j]);
							}

							list2.Add(elem2);
						}
						else if (list2 != null)
							list2.Add(elem1);
					}

					return list2 ?? list1;
				}

				default:
					throw CreateInvalidVisitModeException();
			}
		}

		/// <summary>
		/// Visits list of query elements and applies transformation.
		/// </summary>
		/// <param name="list1">List to visit.</param>
		/// <param name="transformFunc">Transformation function.</param>
		/// <returns>
		/// Return value depends on <paramref name="mode"/> value:
		/// <list type="bullet">
		/// <item><c>null</c> when <paramref name="list1"/> is <c>null</c>;</item>
		/// <item><see cref="VisitMode.ReadOnly"/>: returns input list <paramref name="list1"/> instance;</item>
		/// <item><see cref="VisitMode.Modify"/>: returns input list <paramref name="list1"/> instance, could contain inplace list item replacements;</item>
		/// <item><see cref="VisitMode.Transform"/>: returns new list instance when there were changes to list items; otherwise returns original list.</item>
		/// </list>
		/// </returns>
		[return: NotNullIfNotNull(nameof(list1))]
		protected List<T>? VisitElements<T>(List<T>? list1, VisitMode mode, Func<T, T> transformFunc)
			where T : class
		{
			if (list1 == null)
				return null;

			switch (mode)
			{
				case VisitMode.ReadOnly:
				{
					foreach (var t in list1)
					{
						_ = transformFunc(t);
					}

					return list1;
				}

				case VisitMode.Modify:
				{
					for (var i = 0; i < list1.Count; i++)
					{
						list1[i] = transformFunc(list1[i]);
					}

					return list1;
				}

				case VisitMode.Transform:
				{
					List<T>? list2 = null;

					for (var i = 0; i < list1.Count; i++)
					{
						var elem1 = list1[i];
						var elem2 = transformFunc(elem1);

						if (!ReferenceEquals(elem1, elem2))
						{
							if (list2 == null)
							{
								list2 = new List<T>(list1.Count);

								for (var j = 0; j < i; j++)
									list2.Add(list1[j]);
							}

							list2.Add(elem2);
						}
						else if (list2 != null)
							list2.Add(elem1);
					}

					return list2 ?? list1;
				}

				default:
					throw CreateInvalidVisitModeException();
			}
		}

		/// <summary>
		/// Visits list of arrays of query elements.
		/// </summary>
		/// <returns>
		/// Return value depends on <paramref name="mode"/> value:
		/// <list type="bullet">
		/// <item><c>null</c> when <paramref name="list1"/> is <c>null</c>;</item>
		/// <item><see cref="VisitMode.ReadOnly"/>: returns input list <paramref name="list1"/> instance;</item>
		/// <item><see cref="VisitMode.Modify"/>: returns input list <paramref name="list1"/> instance, could contain inplace list item replacements;</item>
		/// <item><see cref="VisitMode.Transform"/>: returns new list instance when there were changes to list items; otherwise returns original list.</item>
		/// </list>
		/// </returns>
		[return: NotNullIfNotNull(nameof(list1))]
		protected List<T[]>? VisitListOfArrays<T>(List<T[]>? list1, VisitMode mode)
			where T : class, IQueryElement
		{
			if (list1 == null)
				return null;

			switch (mode)
			{
				case VisitMode.ReadOnly:
				{
					foreach (var t in list1)
					{
						_ = VisitElements(t, VisitMode.ReadOnly);
					}

					return list1;
				}
				case VisitMode.Modify:
				{
					for (var i = 0; i < list1.Count; i++)
					{
						list1[i] = VisitElements(list1[i], VisitMode.Modify);
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

						if (elem1 != elem2)
						{
							if (list2 == null)
							{
								list2 = new List<T[]>(list1.Count);

								for (var j = 0; j < i; j++)
								{
									list2.Add(list1[j].ToArray());
								}
							}

							list2.Add(elem2);
						}
						else if (list2 != null)
						{
							list2.Add(elem1);
						}
					}

					return list2 ?? list1;
				}

				default:
					throw CreateInvalidVisitModeException();
			}
		}

		/// <summary>
		/// Visits list of list of query elements.
		/// </summary>
		/// <returns>
		/// Return value depends on <paramref name="mode"/> value:
		/// <list type="bullet">
		/// <item><c>null</c> when <paramref name="list1"/> is <c>null</c>;</item>
		/// <item><see cref="VisitMode.ReadOnly"/>: returns input list <paramref name="list1"/> instance;</item>
		/// <item><see cref="VisitMode.Modify"/>: returns input list <paramref name="list1"/> instance, could contain inplace list item replacements;</item>
		/// <item><see cref="VisitMode.Transform"/>: returns new list instance when there were changes to list items; otherwise returns original list.</item>
		/// </list>
		/// </returns>
		[return: NotNullIfNotNull(nameof(list1))]
		protected List<List<T>>? VisitListOfLists<T>(List<List<T>>? list1, VisitMode mode)
			where T : class, IQueryElement
		{
			if (list1 == null)
				return null;

			switch (mode)
			{
				case VisitMode.ReadOnly:
				{
					foreach (var t in list1)
					{
						_ = VisitElements(t, VisitMode.ReadOnly);
					}

					return list1;
				}
				case VisitMode.Modify:
				{
					for (var i = 0; i < list1.Count; i++)
					{
						list1[i] = VisitElements(list1[i], VisitMode.Modify);
					}

					return list1;
				}
				case VisitMode.Transform:
				{
					List<List<T>>? list2 = null;

					for (var i = 0; i < list1.Count; i++)
					{
						var elem1 = list1[i];
						var elem2 = VisitElements(elem1, VisitMode.Transform);

						if (elem1 != elem2)
						{
							if (list2 == null)
							{
								list2 = new List<List<T>>(list1.Count);

								for (var j = 0; j < i; j++)
								{
									list2.Add(list1[j].ToList());
								}
							}

							list2.Add(elem2);
						}
						else if (list2 != null)
						{
							list2.Add(elem1);
						}
					}

					return list2 ?? list1;
				}

				default:
					throw CreateInvalidVisitModeException();
			}
		}

		#endregion

	}
}
