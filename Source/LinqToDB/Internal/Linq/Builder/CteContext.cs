using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	internal sealed class CteContext : BuildContextBase
	{
		public Expression CteExpression { get; set; }

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

		readonly Dictionary<Expression, SqlPlaceholderExpression>                               _knownMap     = new(ExpressionEqualityComparer.Instance);
		readonly Dictionary<Expression, (SqlField field, SqlPlaceholderExpression placeholder)> _fieldsMap    = new(ExpressionEqualityComparer.Instance);
		readonly Dictionary<Expression, SqlPlaceholderExpression>                               _recursiveMap = new(ExpressionEqualityComparer.Instance);
		readonly Dictionary<Expression, SqlPlaceholderExpression>                               _traverseMap  = new(ExpressionEqualityComparer.Instance);

		readonly Dictionary<MemberExpression, SqlPlaceholderExpression> _virtualFieldToCteFieldPlaceholder = new(ExpressionEqualityComparer.Instance);
		readonly Dictionary<SqlPlaceholderExpression, MemberExpression> _placeholderToVirtualField         = new(ExpressionEqualityComparer.Instance);

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

			_isRecursiveCall = true;

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
			if (flags.IsRoot() || flags.IsAssociationRoot() || flags.IsExtractProjection() || flags.IsExpand())
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
					var field = new SqlField(new DbDataType(path.Type), TableLikeHelpers.GenerateColumnAlias(path) ?? "field", path.Type.IsNullableOrReferenceType);

					newPlaceholder = ExpressionBuilder.CreatePlaceholder((SelectQuery?)null, field, path, trackingPath: path);
					_recursiveMap[path] = newPlaceholder;
				}

				return newPlaceholder;
			}

			InitQuery();

			if (SubQueryContext == null || CteInnerQueryContext == null)
				throw new InvalidOperationException();

			if (SequenceHelper.IsSpecialProperty(path, this))
			{
				if (_virtualFieldToCteFieldPlaceholder.TryGetValue((MemberExpression)path, out var placeholder))
					return placeholder;
				return path;
			}

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
			if (path != null)
			{
				if (_fieldsMap.TryGetValue(path, out var pair))
				{
					return pair.placeholder;
				}

				if (_knownMap.TryGetValue(path, out var knownPlaceholder))
				{
					return knownPlaceholder;
				}
			}

			if (SubQueryContext != null)
				placeholder = Builder.UpdateNesting(SubQueryContext, placeholder);

			var traversed = Builder.BuildTraverseExpression(placeholder.Path);

			if (_traverseMap.TryGetValue(traversed, out var foundPlaceholder))
			{
				return foundPlaceholder;
			}

			if (placeholder.Sql is not SqlColumn column)
				throw new InvalidOperationException("Invalid SQL.");

			if (CteInnerQueryContext != null)
			{
				if (CteClause.Fields.Count < CteInnerQueryContext.SelectQuery.Select.Columns.Count)
				{
					// Add missing fields to CteClause.Fields
					for (int i = CteClause.Fields.Count; i < CteInnerQueryContext.SelectQuery.Select.Columns.Count; i++)
					{
						var innerColumn        = CteInnerQueryContext.SelectQuery.Select.Columns[i];
						var alias              = TableLikeHelpers.GenerateColumnAlias(innerColumn) ?? "field";
						var dataType           = QueryHelper.GetDbDataType(innerColumn, MappingSchema);
						var nullabilityContext = NullabilityContext.GetContext(CteInnerQueryContext.SelectQuery);
						var isNullable         = innerColumn.CanBeNullable(nullabilityContext);
						var missingField       = new SqlField(dataType, alias, isNullable);
						CteClause.Fields.Add(missingField);
					}
				}
			}

			SqlField? field = null;

			var index = CteInnerQueryContext == null ? -1 : CteInnerQueryContext.SelectQuery.Select.Columns.IndexOf(column);

			if (index >= 0 && index < CteClause.Fields.Count)
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
				if (_recursiveMap.TryGetValue(path ?? placeholder.Path, out var recursivePlaceholder))
				{
					recursiveField = (SqlField)recursivePlaceholder.Sql;
				}
			}

			if (field == null)
			{
				var nullabilityContext = NullabilityContext.GetContext(CteInnerQueryContext?.SelectQuery);
				var isNullable         = placeholder.Sql.CanBeNullable(nullabilityContext);

				var alias    = TableLikeHelpers.GenerateColumnAlias(path ?? placeholder.Path!) ?? TableLikeHelpers.GenerateColumnAlias(placeholder.Sql);
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

			_fieldsMap[traversed]         = (field, newPlaceholder);
			_knownMap[newPlaceholderPath] = newPlaceholder;

			_traverseMap[traversed] = newPlaceholder;

			return newPlaceholder;
		}

		public MemberExpression RegisterVirtualField(Expression expression)
		{
			InitQuery();

			if (SubQueryContext == null)
				throw new InvalidOperationException("Context is not initialized.");

			var builtExpression = Builder.BuildSqlExpression(CteInnerQueryContext, expression);

			if (builtExpression is not SqlPlaceholderExpression placeholder)
				throw new InvalidOperationException("Expression is not a placeholder.");

			var updatedNesting = Builder.UpdateNesting(SubQueryContext!, placeholder);

			if (updatedNesting.SelectQuery != SubQueryContext.SelectQuery)
			{
				var isInvalid = true;
				if (placeholder.Sql is SqlField field)
				{
					foreach (var map in _fieldsMap.Values)
					{
						if (map.field == field)
						{
							isInvalid = false;
							placeholder = map.placeholder;
							break;
						}
					}
				}

				if (isInvalid)
					throw new InvalidOperationException("Placeholder belongs to different context.");
			}

			var resolvedFieldPlaceholder = RegisterField(null, updatedNesting);
			if (resolvedFieldPlaceholder == null) 
				throw new InvalidOperationException();

			if (_placeholderToVirtualField.TryGetValue(resolvedFieldPlaceholder, out var virtualField))
			{
				return virtualField;
			}

			var cteFieldName = ((SqlField)resolvedFieldPlaceholder.Sql).Name;

			var fieldName = $"$v[{_virtualFieldToCteFieldPlaceholder.Count.ToString(CultureInfo.InvariantCulture)}]-{cteFieldName}";

			virtualField = SequenceHelper.CreateSpecialProperty(SequenceHelper.CreateRef(this), placeholder.Type, fieldName);
			_placeholderToVirtualField[resolvedFieldPlaceholder] = virtualField;
			_virtualFieldToCteFieldPlaceholder[virtualField] = resolvedFieldPlaceholder;

			return virtualField;
		}

		sealed class CteProxy : BuildProxyBase<CteContext>
		{
			public CteProxy(CteContext ownerContext, Expression? currentPath, Expression innerExpression)
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
				var field = OwnerContext.RegisterField(path, placeholder);
				field = field.WithType(placeholder.Type);

				return field;
			}

			public override BuildProxyBase<CteContext> CreateProxy(CteContext ownerContext, IBuildContext buildContext, Expression? currentPath, Expression innerExpression)
			{
				return new CteProxy(ownerContext, currentPath, innerExpression);
			}
		}

		public override IBuildContext Clone(CloningContext context)
		{
			var newContext = new CteContext(TranslationModifier, Builder, ElementType, SelectQuery);

			context.RegisterCloned(this, newContext);

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

			newContext.SelectQuery          = context.CloneElement(SelectQuery);
			newContext.SubQueryContext      = context.CloneContext(SubQueryContext);
			newContext.CteInnerQueryContext = context.CloneContext(CteInnerQueryContext);
			newContext.CteClause            = context.CloneElement(CteClause);
			newContext.CteExpression        = context.CloneExpression(CteExpression);

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

		public SqlPlaceholderExpression GetFieldPlaceholder(string fieldName)
		{
			foreach (var map in _fieldsMap.Values)
			{
				if (string.Equals(map.field.Name, fieldName, StringComparison.Ordinal))
				{
					return map.placeholder;
				}
			}

			throw new InvalidOperationException($"Field placeholder not found for field: {fieldName}");
		}
	}
}
