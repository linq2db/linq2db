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
			SelectQuery          = cteInnerQueryContext.SelectQuery;
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
				correctedPath = Builder.ConvertToSqlExpr(CteInnerQueryContext, correctedPath, flags);

				if (!flags.HasFlag(ProjectFlags.Test))
				{
					correctedPath = SequenceHelper.CorrectTrackingPath(correctedPath, this);

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

		public override IBuildContext Clone(CloningContext context)
		{
			throw new NotImplementedException();
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
