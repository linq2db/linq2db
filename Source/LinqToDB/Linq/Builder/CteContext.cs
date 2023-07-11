using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;
	using Common;
	using LinqToDB.Expressions;

	internal class CteContext : BuildContextBase
	{
		public override Expression? Expression { get; }

		public IBuildContext?   CteInnerQueryContext { get; private set; }
		public SubQueryContext? SubqueryContext      { get; private set; }
		public CteClause        CteClause            { get; }

		ContextRefExpression CteContextRef { get; }

		public CteContext(ExpressionBuilder builder, IBuildContext? cteInnerQueryContext, CteClause cteClause, Expression cteExpression) 
			: base(builder, cteClause.ObjectType, cteInnerQueryContext?.SelectQuery ?? new SelectQuery())
		{
			CteInnerQueryContext = cteInnerQueryContext; 
			CteClause            = cteClause;
			Expression           = cteExpression;

			var elementType = ExpressionBuilder.GetEnumerableElementType(cteExpression.Type);

			CteContextRef = new ContextRefExpression(elementType, this);
		}

		Dictionary<Expression, SqlPlaceholderExpression> _knownMap = new (ExpressionEqualityComparer.Instance);
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
			try
			{
				var cteInnerQueryContext = Builder.BuildSequence(cteBuildInfo);

				CteInnerQueryContext = cteInnerQueryContext;
				CteClause.Body       = cteInnerQueryContext.SelectQuery;
				SelectQuery          = cteInnerQueryContext.SelectQuery;
				SubqueryContext      = new SubQueryContext(cteInnerQueryContext);

				if (_recursiveMap.Count > 0)
				{
					var subQueryExpr = new ContextRefExpression(SubqueryContext.ElementType, SubqueryContext);
					var all = Builder.ConvertToSqlExpr(SubqueryContext, subQueryExpr, ProjectFlags.SQL);

					var cteExpr = subQueryExpr.WithContext(this);

					foreach (var pair in _recursiveMap)
					{
						_knownMap.Add(pair.Key, pair.Value);
					}
				

					PostProcessExpression(all, cteExpr);
					var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(all);
				}

			}
			finally
			{
				_isRecursiveCall = false;
			}
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
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
						var newField = new SqlField(new DbDataType(path.Type), TableLikeHelpers.GenerateColumnAlias(path), true);
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
				correctedPath = Builder.ConvertToSqlExpr(SubqueryContext, correctedPath, flags);

				if (!flags.HasFlag(ProjectFlags.Test))
				{
					return PostProcessExpression(correctedPath, path);
				}
			}

			return correctedPath;
		}

		Expression PostProcessExpression(Expression correctedPath, Expression subqueryPath)
		{
			correctedPath = SequenceHelper.CorrectTrackingPath(correctedPath, subqueryPath);

			correctedPath = RemapRecursive(correctedPath);
			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(correctedPath);

			var remapped = TableLikeHelpers.RemapToFields(SubqueryContext, null, CteClause.Fields, _knownMap, correctedPath,
				placeholders);
			return remapped;
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

		public override IBuildContext Clone(CloningContext context)
		{
			var newContext = new CteContext(Builder, context.CloneContext(CteInnerQueryContext),
				context.CloneElement(CteClause), context.CloneExpression(Expression!));

			return newContext;
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			throw new InvalidOperationException();
		}

		public override SqlStatement GetResultStatement()
		{
			throw new InvalidOperationException();
		}
		
	}
}
