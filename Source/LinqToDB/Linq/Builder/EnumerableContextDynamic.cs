using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	sealed class EnumerableContextDynamic : BuildContextBase
	{
		readonly Expression[]     _expressionRows;

		public override Expression?    Expression    { get; }
		public override MappingSchema  MappingSchema => Builder.MappingSchema;
		public          SqlValuesTable Table         { get; }

		public EnumerableContextDynamic(TranslationModifier translationModifier, IBuildContext parent, ExpressionBuilder builder, Expression[] expressionRows, SelectQuery query, Type elementType)
			: base(translationModifier, builder, elementType, query)
		{
			_expressionRows = expressionRows;
			Parent          = parent;

			Table      = new SqlValuesTable();
			Expression = null;

			SelectQuery.From.Table(Table);
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
			{
				if (flags.IsRoot() || flags.IsTable() || flags.IsAssociationRoot() || flags.IsTraverse() || flags.IsAggregationRoot() || flags.IsExtractProjection() || flags.IsMemberRoot())
					return path;

				if (MappingSchema.IsScalarType(ElementType) || Builder.CurrentDescriptor != null)
				{
					var dbType = Builder.CurrentDescriptor?.GetDbDataType(true) ?? MappingSchema.GetDbDataType(ElementType);
					if (dbType.DataType != DataType.Undefined || ElementType.IsEnum)
					{
						if (path.Type != ElementType)
						{
							path = ((ContextRefExpression)path).WithType(ElementType);
						}

						var specialProp = SequenceHelper.CreateSpecialProperty(path, ElementType, "item");

						return specialProp;
					}
				}


				if (flags.IsExpression())
				{
					if (_expressionRows.Length == 0)
					{
						//var result = Builder.BuildEntityExpression(MappingSchema, path, ElementType, membersOrdered.ToList());
						return ExpressionBuilder.CreateSqlError(path);
					}

					var helper     = new MergeProjectionHelper(Builder, MappingSchema);

					var projectionWithPath = new Expression[_expressionRows.Length];
					for (var i = 0; i < _expressionRows.Length; i++)
					{
						var rowProjection = _expressionRows[i];
						if (!helper.BuildProjectionExpression(rowProjection, Parent!, out var pathProjection, out var placeholders, out var foundEager, out var errorExpr))
						{
							return errorExpr;
						}

						projectionWithPath[i] = pathProjection;
					}

					var projection = projectionWithPath[0];

					for (var i = 1; i < projectionWithPath.Length; i++)
					{
						var rowProjection = projectionWithPath[i];
						if (!helper.TryMergeProjections(rowProjection, rowProjection, flags, out var merged))
						{
							return new SqlErrorExpression(path, $"Could not decide which construction type to use `query.Select(x => new {projection.Type.Name} {{ ... }})` to specify projection.",
								path.Type);
						}

						projection = merged;
					}

					return projection;
				}

				return path;
			}

			if (path is not MemberExpression member)
				return ExpressionBuilder.CreateSqlError(path);

			var placeholder = BuildFieldPlaceholder(member, flags);

			if (placeholder == null)
				return ExpressionBuilder.CreateSqlError(path);

			return placeholder;
		}

		List<(Expression path, ColumnDescriptor? descriptor, SqlPlaceholderExpression placeholder)> _fieldsMap = new ();

		SqlPlaceholderExpression? BuildFieldPlaceholder(MemberExpression memberExpression, ProjectFlags flags)
		{
			var currentDescriptor = Builder.CurrentDescriptor;

			for (var i = 0; i < _fieldsMap.Count; i++)
			{
				var (path, descriptor, placeholder) = _fieldsMap[i];
				if (descriptor == currentDescriptor && ExpressionEqualityComparer.Instance.Equals(path, memberExpression))
					return placeholder;
			}

			var translations = new List<Expression>();
			var isSpecial    = SequenceHelper.IsSpecialProperty(memberExpression, memberExpression.Type, "item");

			for (var i = 0; i < _expressionRows.Length; i++)
			{
				var rowProjection = _expressionRows[i];
				var projected     = isSpecial ? rowProjection : Builder.Project(Parent!, memberExpression, null, 0, flags, rowProjection, false);
				var translated    = Builder.BuildSqlExpression(Parent, projected);
				translations.Add(translated);
			}

			var firstTranslated = translations.OfType<SqlPlaceholderExpression>().FirstOrDefault();

			if (firstTranslated == null)
			{
				return null;
			}

			var fieldName = GenerateFieldName(memberExpression);

			if (fieldName == null)
			{
				return null;
			}

			var dbDataType = currentDescriptor?.GetDbDataType(true) ?? QueryHelper.GetDbDataType(firstTranslated.Sql, MappingSchema);

			var field = new SqlField(dbDataType, fieldName, true);
			var fieldPlaceholder = ExpressionBuilder.CreatePlaceholder(this, field, memberExpression);

			_fieldsMap.Add((memberExpression, currentDescriptor, fieldPlaceholder));

			Table.AddField(field);

			// fill rows
			if (Table.Rows == null)
			{
				Table.Rows = new ();
				Table.Rows.AddRange(Enumerable.Range(0, translations.Count).Select(_ => new List<ISqlExpression>()));
			}

			for (var i = 0; i < translations.Count; i++)
			{
				var            translated = translations[i];
				ISqlExpression sql;
				if (translated is SqlPlaceholderExpression sqlPlaceholder)
				{
					sql = sqlPlaceholder.Sql;
				}
				else
				{
					sql = new SqlValue(dbDataType, null);
				}

				Table.Rows[i].Add(sql);
			}

			return fieldPlaceholder;
		}

		static string? GenerateFieldName(Expression expr)
		{
			var     current = expr;
			string? fieldName   = null;
			while (current is MemberExpression memberExpression)
			{
				if (fieldName != null)
					fieldName = memberExpression.Member.Name + "_" + fieldName;
				else
					fieldName = memberExpression.Member.Name;
				current = memberExpression.Expression;
			}

			return fieldName;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new EnumerableContextDynamic(TranslationModifier, context.CloneContext(Parent)!, Builder, _expressionRows.Select(e => context.CloneExpression(e)).ToArray(), 
				context.CloneElement(SelectQuery),
				ElementType);
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		public override void SetAlias(string? alias)
		{
			if (SelectQuery.Select.Columns.Count == 1)
			{
				var sqlColumn = SelectQuery.Select.Columns[0];
				if (sqlColumn.RawAlias == null)
					sqlColumn.Alias = alias;
			}
		}

		public override SqlStatement GetResultStatement()
		{
			return new SqlSelectStatement(SelectQuery);
		}
	}
}
