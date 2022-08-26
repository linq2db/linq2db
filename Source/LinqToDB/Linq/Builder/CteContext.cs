using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	internal class CteContext : IBuildContext
	{
#if DEBUG
		public string SqlQueryText => SelectQuery == null ? "" : SelectQuery.SqlText;
		public string Path         => this.GetPath();
		public int    ContextId    { get; }
#endif
		public SelectQuery? SelectQuery
		{
			get => CteInnerQueryContext?.SelectQuery;
			set { }
		}

		public SqlStatement?  Statement { get; set; }
		public IBuildContext? Parent    { get; set; }

		public ExpressionBuilder Builder              { get; }
		public Expression?       Expression           { get; }

		public IBuildContext? CteInnerQueryContext { get; private set; }
		public IBuildContext? SubqueryContext { get; private set; }
		public CteClause      CteClause            { get; }

		public CteContext(ExpressionBuilder builder, IBuildContext? cteInnerQueryContext, CteClause cteClause, Expression cteExpression)
		{
			Builder              = builder;
			CteInnerQueryContext = cteInnerQueryContext;
			CteClause            = cteClause;
			Expression           = cteExpression;
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

				var subqueryPathTranslated = Builder.MakeExpression(subqueryPath, projectFlags) as SqlPlaceholderExpression;

				if (subqueryPathTranslated == null)
					throw new LinqException($"'{subqueryPath}' cannot be converted to SQL.");

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
				if (path is MemberExpression)
				{
					if (!_recursiveMap.TryGetValue(path, out var newPlaceholder))
					{
						var index = CteClause.Fields?.Length ?? 0;
						var field = CteClause.RegisterFieldMapping(index, () =>
						{
							var newField = new SqlField(path.Type, GenerateColumnAlias(path), true);
							return newField;
						});

						newPlaceholder = ExpressionBuilder.CreatePlaceholder((SelectQuery?)null, field, path, index: index);
						_recursiveMap[path] = newPlaceholder;
					}

					return newPlaceholder;

				}
				return path;
			}

			/*
			if (flags.HasFlag(ProjectFlags.CompleteQuery))
			{
				if ((CteClause.Fields?.Length ?? 0) == 0)
				{
					CteInnerQueryContext.SelectQuery.Select.AddNew(new SqlValue(1), "any");
					CteClause.RegisterFieldMapping(0, () => new SqlField(typeof(int), "any", false));
					return path;
				}

				return path;
			}
			*/

			//var correctedPath = SequenceHelper.CorrectExpression(path, this, CteInnerQueryContext);
			var correctedPath = SequenceHelper.CorrectExpression(path, this, SubqueryContext!);
			if (!ReferenceEquals(correctedPath, path))
			{
				correctedPath = Builder.MakeExpression(correctedPath, flags);
				correctedPath = RemapRecursive(correctedPath);


				if (!flags.HasFlag(ProjectFlags.Test))
				{
					var placeholders = ExpressionBuilder.CollectPlaceholders(correctedPath)
						.Where(p => p.SelectQuery == SubqueryContext!.SelectQuery && p.Index != null).ToList();

					var remapped = RemapToFields(correctedPath, placeholders);
					return remapped;

				}
			}

			return correctedPath;
		}

		static string? GenerateColumnAlias(Expression expr)
		{
			var     current = expr;
			string? alias   = null;
			while (current is MemberExpression memberExpression)
			{
				if (alias != null)
					alias = memberExpression.Member.Name + "_" + alias;
				else
					alias = memberExpression.Member.Name;
				current = memberExpression.Expression;
			}

			return alias;
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

		Expression RemapToFields(Expression expression, List<SqlPlaceholderExpression> placeholders)
		{
			if (placeholders.Count == 0)
				return expression;

			var needsTransformation = false;

			var newPlaceholders = new SqlPlaceholderExpression[placeholders.Count];

			for (var index = 0; index < placeholders.Count; index++)
			{
				var placeholder = placeholders[index];
				if (!_knownMap.TryGetValue(placeholder, out var newPlaceholder))
				{
					var field = CteClause.RegisterFieldMapping(placeholder.Index!.Value, () =>
					{
						var newField = new SqlField(placeholder.Type, GenerateColumnAlias(placeholder.Path),
							placeholder.Sql.CanBeNull);
						return newField;
					});

					newPlaceholder         = ExpressionBuilder.CreatePlaceholder(SubqueryContext!.SelectQuery, field, placeholder.Path, index: placeholder.Index);

					_knownMap[placeholder]    = newPlaceholder;
					// Cycle mapping
					_knownMap[newPlaceholder] = newPlaceholder;
				}

				if (!ReferenceEquals(newPlaceholder, placeholder))
					needsTransformation = true;

				newPlaceholders[index] = newPlaceholder;
			}

			if (!needsTransformation)
				return expression;

			var transformed = expression.Transform((placeholders, newPlaceholders), (ctx, e) =>
			{
				if (e.NodeType == ExpressionType.Extension && e is SqlPlaceholderExpression placeholder)
				{
					var index = ctx.placeholders.IndexOf(placeholder);
					if (index >= 0)
					{
						return ctx.newPlaceholders[index];
					}
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
			throw new NotImplementedException();
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
