using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;
	using LinqToDB.Expressions;

	internal class CteContext : IBuildContext
	{
#if DEBUG
		public string SqlQueryText => CteInnerQueryContext == null ? "" : SelectQuery.SqlText;
		public string Path         => this.GetPath();
		public int    ContextId    { get; }
#endif
		public SelectQuery SelectQuery
		{
			get => CteInnerQueryContext?.SelectQuery ?? new SelectQuery();
			set { }
		}

		public SqlStatement?  Statement { get; set; }
		public IBuildContext? Parent    { get; set; }

		public ExpressionBuilder Builder              { get; }
		public Expression?       Expression           { get; }

		public IBuildContext?   CteInnerQueryContext { get; private set; }
		public SubQueryContext? SubqueryContext      { get; private set; }
		public CteClause        CteClause            { get; }

		ContextRefExpression CteContextRef { get; }

		public CteContext(ExpressionBuilder builder, IBuildContext? cteInnerQueryContext, CteClause cteClause, Expression cteExpression)
		{
			Builder              = builder;
			CteInnerQueryContext = cteInnerQueryContext; 
			CteClause            = cteClause;
			Expression           = cteExpression;

			var elementType = ExpressionBuilder.GetEnumerableElementType(cteExpression.Type);

			CteContextRef = new ContextRefExpression(elementType, this);

#if DEBUG
			ContextId = Builder.GenerateContextId();
#endif
		}

		public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			throw new NotImplementedException();
		}

		public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
		{
			throw new NotImplementedException();
		}

		public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		Dictionary<SqlPlaceholderExpression, SqlPlaceholderExpression> _knownMap = new (ExpressionEqualityComparer.Instance);
		Dictionary<Expression, SqlPlaceholderExpression> _recursiveMap = new (ExpressionEqualityComparer.Instance);

		bool _isRecursiveCall;

		public void InitQuery()
		{
			if (_isRecursiveCall)
				return;

			if (CteInnerQueryContext != null)
				return;

			var cteBuildInfo = new BuildInfo((IBuildContext?)null, Expression!, new SelectQuery());
			_isRecursiveCall = true;
			var cteInnerQueryContext = Builder.BuildSequence(cteBuildInfo);
			_isRecursiveCall = false;

			CteInnerQueryContext = cteInnerQueryContext;
			CteClause.Body       = cteInnerQueryContext.SelectQuery;
			SubqueryContext      = new SubQueryContext(cteInnerQueryContext);

			foreach (var mapped in _recursiveMap.OrderBy(m => m.Value.Index).ToList())
			{
				var subqueryPath = SequenceHelper.CorrectExpression(mapped.Value.Path, this, SubqueryContext);
				var projectFlags = ProjectFlags.SQL;

				var subqueryPathTranslated = Builder.MakeExpression(SubqueryContext, subqueryPath, projectFlags) as SqlPlaceholderExpression;

				if (subqueryPathTranslated == null)
					throw new LinqException($"'{subqueryPath}' cannot be converted to SQL.");

				CteClause.UpdateIndex(subqueryPathTranslated.Index!.Value, (SqlField)mapped.Value.Sql);

				var newPlaceholder = ExpressionBuilder.CreatePlaceholder(SubqueryContext!.SelectQuery, mapped.Value.Sql, subqueryPathTranslated.Path, index: subqueryPathTranslated.Index);
				_knownMap[subqueryPathTranslated] = newPlaceholder;
				_knownMap[newPlaceholder]         = newPlaceholder; // Cycle mapping
				_recursiveMap[subqueryPath]       = newPlaceholder;
			}
		}

		public Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot) || flags.HasFlag(ProjectFlags.Expand))
				return path;

			if (_isRecursiveCall)
			{
				if (!_recursiveMap.TryGetValue(path, out var newPlaceholder))
				{
					var index = CteClause.Fields.Count;
					var field = TableLikeHelpers.RegisterFieldMapping(CteClause.Fields, index, () =>
					{
						var newField = new SqlField(path.Type, TableLikeHelpers.GenerateColumnAlias(path), true);
						return newField;
					});

					newPlaceholder = ExpressionBuilder.CreatePlaceholder((SelectQuery?)null, field, path, index: index, trackingPath: path);
					_recursiveMap[path] = newPlaceholder;
				}

				return newPlaceholder;
			}

			InitQuery();

			if (SubqueryContext == null || CteInnerQueryContext == null)
				throw new InvalidOperationException();

			var subqueryPath  = SequenceHelper.CorrectExpression(path, this, SubqueryContext);
			var correctedPath = subqueryPath;

			if (!ReferenceEquals(subqueryPath, path))
			{
				correctedPath = Builder.ConvertToSqlExpr(CteInnerQueryContext, correctedPath, flags);

				if (!flags.HasFlag(ProjectFlags.Test))
				{
					correctedPath = SequenceHelper.CorrectTrackingPath(correctedPath, SubqueryContext, this);

					var memberPath = TableLikeHelpers.GetMemberPath(subqueryPath);
					correctedPath = Builder.UpdateNesting(SubqueryContext, correctedPath);
					correctedPath = RemapRecursive(correctedPath);
					var placeholders = ExpressionBuilder.CollectPlaceholders2(correctedPath, memberPath).ToList();

					var remapped = TableLikeHelpers.RemapToFields(SubqueryContext, null, CteClause.Fields, _knownMap, correctedPath, placeholders);
					return remapped;
				}
			}

			return correctedPath;
		}

		Expression RemapRecursive(Expression expression)
		{
			if (_recursiveMap.Count == 0)
				return expression;

			var transformed = expression.Transform(_recursiveMap, static (map, e) =>
			{
				if (map.TryGetValue(e, out var newPlaceholder))
				{
					return newPlaceholder;
				}

				return e;
			});

			return transformed;
		}

		public IBuildContext Clone(CloningContext context)
		{
			throw new NotImplementedException();
		}

		public void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			throw new NotImplementedException();
		}

		public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			throw new NotImplementedException();
		}

		public IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			return null;
		}

		public int ConvertToParentIndex(int index, IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public void SetAlias(string? alias)
		{
			throw new NotImplementedException();
		}

		public ISqlExpression? GetSubQuery(IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public SqlStatement GetResultStatement()
		{
			throw new NotImplementedException();
		}

		public void CompleteColumns()
		{
			throw new NotImplementedException();
		}
	}
}
