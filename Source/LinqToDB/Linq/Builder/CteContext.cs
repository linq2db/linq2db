using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common;

	using LinqToDB.Expressions;
	using LinqToDB.Mapping;

using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	internal class CteContext : BuildContextBase
	{
		public Expression CteExpression { get; set;  }

		public override Expression?   Expression    => CteExpression;
		public override MappingSchema MappingSchema => CteInnerQueryContext?.MappingSchema ?? Builder.MappingSchema;

		public IBuildContext?   CteInnerQueryContext { get; private set; }
		public SubQueryContext? SubQueryContext      { get; private set; }
		public CteClause        CteClause            { get; private set; }

		public CteContext(TranslationModifier translationModifier, ExpressionBuilder builder, IBuildContext? cteInnerQueryContext, CteClause cteClause, Expression cteExpression)
			: this(translationModifier, builder, cteClause.ObjectType, cteInnerQueryContext?.SelectQuery ?? new SelectQuery())
		{
			CteInnerQueryContext = cteInnerQueryContext;
			CteClause            = cteClause;
			CteExpression        = cteExpression;
		}

		CteContext(TranslationModifier translationModifier, ExpressionBuilder builder, Type objectType, SelectQuery selectQuery)
			: base(translationModifier, builder, objectType, selectQuery)
		{
			CteClause     = default!;
			CteExpression = default!;
		}

		Dictionary<Expression, SqlPlaceholderExpression>                               _knownMap     = new (ExpressionEqualityComparer.Instance);
		Dictionary<Expression, (SqlField field, SqlPlaceholderExpression placeholder)> _fieldsMap    = new(ExpressionEqualityComparer.Instance);
		Dictionary<Expression, SqlPlaceholderExpression>                               _recursiveMap = new (ExpressionEqualityComparer.Instance);

		bool _isRecursiveCall;

		public void InitQuery()
		{
			if (CteInnerQueryContext != null)
				return;

			if (_isRecursiveCall)
				return;

			var thisRef = new ContextRefExpression(ElementType, this);

			Builder.PushRecursive(thisRef);

			var cteBuildInfo = new BuildInfo((IBuildContext?)null, Expression!, new SelectQuery());

			_isRecursiveCall         = true;

			var cteInnerQueryContext = Builder.BuildSequence(cteBuildInfo);

			CteInnerQueryContext = cteInnerQueryContext;
			CteClause.Body       = cteInnerQueryContext.SelectQuery;
			SelectQuery          = cteInnerQueryContext.SelectQuery;
			SubQueryContext      = new SubQueryContext(cteInnerQueryContext);

			_isRecursiveCall = false;

			if (_recursiveMap.Count > 0)
			{
				var innerQueryContext = new ContextRefExpression(cteInnerQueryContext.ElementType, cteInnerQueryContext);

				var cteExpr = SequenceHelper.CreateRef(this);
				var proxy   = new CteProxy(this, cteExpr, innerQueryContext);

				var all = Builder.BuildSqlExpression(cteInnerQueryContext, SequenceHelper.CreateRef(proxy));
			}

			Builder.PopRecursive(thisRef);
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
					// For recursive CTE we cannot calculate nullability correctly, so based on path.Type
					var field = new SqlField(new DbDataType(path.Type), TableLikeHelpers.GenerateColumnAlias(path) ?? "field", path.Type.IsNullableType());

					newPlaceholder = ExpressionBuilder.CreatePlaceholder((SelectQuery?)null, field, path, trackingPath: path);
					_recursiveMap[path] = newPlaceholder;
				}

				return newPlaceholder;
			}

			InitQuery();

			if (SubQueryContext == null || CteInnerQueryContext == null)
				throw new InvalidOperationException();

			var subqueryPath  = SequenceHelper.CorrectExpression(path, this, CteInnerQueryContext);
			var correctedPath = subqueryPath;

			if (!ReferenceEquals(subqueryPath, path))
			{
				_isRecursiveCall = true;

				var proxy = new CteProxy(this, path, subqueryPath);

				var buildExpression = Builder.BuildSqlExpression(CteInnerQueryContext, SequenceHelper.CreateRef(proxy));

				_isRecursiveCall = false;

				return buildExpression;
			}

			return correctedPath;
		}

		public SqlPlaceholderExpression RegisterField(Expression? path, SqlPlaceholderExpression placeholder)
		{
			if (_fieldsMap.TryGetValue(placeholder.Path, out var pair))
			{
				return pair.placeholder;
			}

			if (_knownMap.TryGetValue(placeholder.Path, out var knownPlaceholder))
			{
				return knownPlaceholder;
			}

			if (placeholder.Sql is not SqlColumn column)
				throw new InvalidOperationException("Invalid SQL.");

			SqlField? field = null;

			var index = CteInnerQueryContext == null ? -1 : CteInnerQueryContext.SelectQuery.Select.Columns.IndexOf(column);

			if (index >= 0 && CteClause.Fields.Count != index)
			{
				field = CteClause.Fields[index];

				foreach (var map in _fieldsMap.Values)
				{
					if (map.field == field)
					{
						return map.placeholder;
					}
				}
			}

			SqlField? recursiveField = null;

			if (field == null)
			{
				if (_recursiveMap.TryGetValue(placeholder.Path, out var recursivePlaceholder))
				{
					recursiveField = (SqlField)recursivePlaceholder.Sql;
				}
			}

			if (field == null)
			{
				var nullabilityContext = NullabilityContext.GetContext(CteInnerQueryContext?.SelectQuery);
				var isNullable         = placeholder.Sql.CanBeNullable(nullabilityContext);

				var alias    = TableLikeHelpers.GenerateColumnAlias(placeholder.TrackingPath!) ?? TableLikeHelpers.GenerateColumnAlias(placeholder.Sql);
				var dataType = QueryHelper.GetDbDataType(placeholder.Sql, MappingSchema);

				if (recursiveField != null)
				{
					field           = recursiveField;
					field.CanBeNull = isNullable;
					field.Type      = dataType;
				}
				else
		{
					field = new SqlField(dataType, alias, isNullable);
				}

				Utils.MakeUniqueNames([field], CteClause.Fields.Where(f => f != null).Select(t => t.Name), f => f.Name, (f, n, a) =>
				{
					f.Name         = n;
					f.PhysicalName = n;
				}, f => (string.IsNullOrEmpty(f.Name) ? "field" : f.Name) + "_1");

				CteClause.Fields.Add(field);
		}

			var newPlaceholderPath = path;

			if (newPlaceholderPath == null)
			{
				var refExpr = SequenceHelper.CreateRef(this);
				newPlaceholderPath = SequenceHelper.CreateSpecialProperty(refExpr, placeholder.Type, "$" + field.Name);
			}

			var newPlaceholder = ExpressionBuilder.CreatePlaceholder(SelectQuery, field, newPlaceholderPath, index: placeholder.Index);

			_fieldsMap[placeholder.Path] = (field, newPlaceholder);
			_knownMap[newPlaceholderPath] = newPlaceholder;

			return newPlaceholder;
		}

		class CteProxy : BuildProxyBase<CteContext>
		{
			public CteProxy(CteContext ownerContext, Expression currentPath, Expression innerExpression) 
				: base(ownerContext, ownerContext.CteInnerQueryContext!, currentPath, innerExpression)
			{
				if (ownerContext.CteInnerQueryContext == null)
					throw new InvalidOperationException();
			}

		public override IBuildContext Clone(CloningContext context)
		{
				return new CteProxy(context.CloneContext(OwnerContext), context.CloneExpression(CurrentPath), context.CloneExpression(InnerExpression));
			}

			public override Expression HandleTranslated(Expression? path, SqlPlaceholderExpression placeholder)
			{
				var withNesting = OwnerContext.SubQueryContext == null
					? placeholder
					: Builder.UpdateNesting(OwnerContext.SubQueryContext!, placeholder);

				if (path != null)
					withNesting = withNesting.WithPath(path).WithTrackingPath(path);
				else
					withNesting = withNesting.WithPath(withNesting.TrackingPath ?? withNesting.Path);

				var field = OwnerContext.RegisterField(path, withNesting);
				field = field.WithType(placeholder.Type);

				return field;
			}

			public override BuildProxyBase<CteContext> CreateProxy(CteContext ownerContext, IBuildContext buildContext, Expression currentPath, Expression innerExpression)
			{
				return new CteProxy(ownerContext, currentPath, innerExpression);
			}
		}

		public override IBuildContext Clone(CloningContext context)
		{
			var newContext = new CteContext(TranslationModifier, Builder, ElementType, SelectQuery);

			context.RegisterCloned(this, newContext);

			newContext.SubQueryContext      = context.CloneContext(SubQueryContext);
			newContext.CteInnerQueryContext = context.CloneContext(CteInnerQueryContext);
			newContext.CteClause            = context.CloneElement(CteClause);
			newContext.CteExpression        = context.CloneExpression(CteExpression);

			foreach (var fm in _fieldsMap)
			{
				newContext._fieldsMap.Add(context.CloneExpression(fm.Key), (field: context.CloneElement(fm.Value.field), placeholder: context.CloneExpression(fm.Value.placeholder)));
			}

			foreach (var km in _knownMap)
			{
				newContext._knownMap.Add(context.CloneExpression(km.Key), context.CloneExpression(km.Value));
			}

			foreach (var rm in _recursiveMap)
			{
				newContext._recursiveMap.Add(context.CloneExpression(rm.Key), context.CloneExpression(rm.Value));
			}

			return newContext;
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			throw new InvalidOperationException();
		}

		public override SqlStatement GetResultStatement()
		{
			return new SqlSelectStatement(SelectQuery);
		}
	}
}
