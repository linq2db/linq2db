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
		Dictionary<Expression, SqlPlaceholderExpression>? _currentRecursiveProcessingMap;

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

			CteInnerQueryContext = cteInnerQueryContext;
			CteClause.Body       = cteInnerQueryContext.SelectQuery;
			SelectQuery          = cteInnerQueryContext.SelectQuery;
			SubqueryContext      = new SubQueryContext(cteInnerQueryContext);

			_isRecursiveCall = false;

			if (_recursiveMap.Count > 0)
			{
				var subQueryExpr = new ContextRefExpression(SubqueryContext.ElementType, SubqueryContext);
				var buildFlags = ExpressionBuilder.BuildFlags.ForceAssignments |
				                 ExpressionBuilder.BuildFlags.IgnoreNullComparison;

				var all = Builder.BuildSqlExpression(SubqueryContext, subQueryExpr, ProjectFlags.SQL,
					buildFlags : buildFlags);

				var cteExpr = subQueryExpr.WithContext(this);

				PostProcessExpression(all, cteExpr);
			}
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot) || flags.HasFlag(ProjectFlags.ExtractProjection))
				return path;

			if (_isRecursiveCall)
			{
				if (SequenceHelper.IsSameContext(path, this) && _recursiveMap.Count > 0)
				{
					if (_recursiveMap.TryGetValue(path, out var value))
						return value;
					return path;
				}

				if (_knownMap.TryGetValue(path, out var alreadyTranslated))
					return alreadyTranslated;

				if (!_recursiveMap.TryGetValue(path, out var newPlaceholder))
				{
					var field = new SqlField(new DbDataType(path.Type), TableLikeHelpers.GenerateColumnAlias(path) ?? "field", true);

					newPlaceholder = ExpressionBuilder.CreatePlaceholder((SelectQuery?)null, field, path, trackingPath: path);
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
				_isRecursiveCall = true;

				var buildFlags = ExpressionBuilder.BuildFlags.ForceAssignments | ExpressionBuilder.BuildFlags.IgnoreNullComparison;
				correctedPath = Builder.BuildSqlExpression(SubqueryContext, correctedPath, flags.SqlFlag(), buildFlags: buildFlags);

				_isRecursiveCall = false;

				if (!flags.HasFlag(ProjectFlags.Test))
				{
					var postProcessed = PostProcessExpression(correctedPath, path);

					return postProcessed;
				}
			}

			return correctedPath;
		}

		Expression PostProcessExpression(Expression correctedPath, Expression subqueryPath)
		{
			correctedPath = SequenceHelper.CorrectTrackingPath(correctedPath, subqueryPath);

			correctedPath = RemapRecursive(correctedPath);
			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(correctedPath);

			var remapped = TableLikeHelpers.RemapToFields(SubqueryContext!, null, CteClause.Fields, _knownMap, _currentRecursiveProcessingMap, correctedPath,
				placeholders);
			return remapped;
		}

		Expression RemapRecursive(Expression expression)
		{
			if (_recursiveMap.Count == 0)
				return expression;

			var toProcess = _recursiveMap.ToList();

			_currentRecursiveProcessingMap = _recursiveMap;

			_recursiveMap = new (ExpressionEqualityComparer.Instance);

			var toRemap = toProcess.ToDictionary(e => e.Key,
				e =>
				{
					var converted = MakeExpression(e.Key, ProjectFlags.SQL);

					return converted;
				}, ExpressionEqualityComparer.Instance);

			/*
			var transformed = expression.Transform(toRemap, static (map, e) =>
			{
				if (map.TryGetValue(e, out var newPlaceholder))
				{
					return newPlaceholder;
				}

				return e;
			});
			*/

			return expression;
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
