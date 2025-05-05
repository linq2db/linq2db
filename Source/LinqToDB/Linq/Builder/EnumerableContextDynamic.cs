using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	sealed class EnumerableContextDynamic : BuildContextBase
	{
		readonly Expression[] _expressionRows;
		Expression[]?         _expandedExpressionRows;
		SqlErrorExpression?   _expandedErrorExpression;
		Expression? _rowIndexExpression;

		readonly Dictionary<SqlPathExpression, List<(int rowIndex, SqlPlaceholderExpression)>>  ByPathExpressions = new (ExpressionEqualityComparer.Instance);

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

		public Expression EnsureRowIndexCreated(Expression path)
		{
			if (_rowIndexExpression != null)
				return _rowIndexExpression;

			var specialProp = SequenceHelper.CreateSpecialProperty(path, typeof(int), "index");
			var placeholder = BuildFieldPlaceholder(specialProp, null, ProjectFlags.SQL);
			if (placeholder == null)
				throw new InvalidOperationException("Placeholder is null");

			_rowIndexExpression = specialProp;

			return _rowIndexExpression;
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

					if (!TryGetExpandedRows(out var expandedRows, out var error))
					{
						return error;
					}

					var projection = expandedRows[0];

					for (var i = 1; i < expandedRows.Length; i++)
					{
						var rowProjection = expandedRows[i];

						var idx = i;

						var helper = new MergeProjectionHelper(Builder, MappingSchema, (Expression projection1, Expression projection2, out Expression? mergedProjection) =>
						{
							var row1 = EnsureRowIndexCreated(path);
							mergedProjection = Expression.Condition(Expression.Equal(row1, ExpressionInstances.Int32(idx - 1)), projection1, projection2);
							return true;
						});

						if (!helper.TryMergeProjections(projection, rowProjection, flags, out var merged))
						{
							return new SqlErrorExpression(path, $"Could not decide which construction type to use `query.Select(x => new {projection.Type.Name} {{ ... }})` to specify projection.",
								path.Type);
						}

						projection = merged;
					}

					var remapped = RemapToValuesFields(projection, path);

					return remapped;
				}

				return path;
			}

			if (path is not MemberExpression member)
				return ExpressionBuilder.CreateSqlError(path);

			var placeholder = BuildFieldPlaceholder(member, null, flags);

			if (placeholder == null)
				return ExpressionBuilder.CreateSqlError(path);

			return placeholder;
		}

		Expression RemapToValuesFields(Expression projection, Expression path)
		{
			var helper = new ProjectionPathHelper(Builder, MappingSchema, (_, path, expression) =>
			{
				if (expression is SqlPathExpression pathExpr && path is MemberExpression pathMember)
				{
					var placeholder = BuildFieldPlaceholder(pathMember, null, ProjectFlags.SQL);
					if (placeholder == null)
						return new SqlErrorExpression(pathMember);
					return placeholder;
				}

				return expression;
			});

			var newExpression = helper.AnalyseExpression(projection, path);

			// Do it again if fields are placed in other constructions
			newExpression = newExpression.Transform(e =>
			{
				if (e is SqlPathExpression pathExpr)
				{
					var placeholder = BuildFieldPlaceholder(null, pathExpr, ProjectFlags.SQL);
					if (placeholder == null)
						return new SqlErrorExpression(pathExpr);
					return placeholder;
				}

				return e;
			});

			return newExpression;
		}

		bool TryGetExpandedRows([NotNullWhen(true)] out Expression[]? expandedRows, [NotNullWhen(false)] out SqlErrorExpression? error)
		{
			if (_expandedExpressionRows != null)
			{
				expandedRows = _expandedExpressionRows;
				error        = null;
				return true;
			}

			if (_expandedErrorExpression != null)
			{
				expandedRows = null;
				error        = _expandedErrorExpression;
				return false;
			}

			var helper = new MergeProjectionHelper(Builder, MappingSchema);

			var expandedExpressionRows = new Expression[_expressionRows.Length];
			for (var i = 0; i < _expressionRows.Length; i++)
			{
				var rowProjection = _expressionRows[i];
				if (!helper.BuildProjectionExpression(rowProjection, Parent!, out var pathProjection, out var placeholders, out var foundEager, out var errorExpr))
				{
					error                    = errorExpr;
					expandedRows             = null;
					_expandedErrorExpression = errorExpr;
					return false;
				}

				foreach (var (placeholder, path) in placeholders)
				{
					var pathExpr = new SqlPathExpression(path, placeholder.Type);
					if (!ByPathExpressions.TryGetValue(pathExpr, out var list))
					{
						list = [];
						ByPathExpressions.Add(pathExpr, list);
					}
					
					list.Add((i, placeholder));
				}

				expandedExpressionRows[i] = pathProjection;
			}

			error                   = null;
			_expandedExpressionRows = expandedExpressionRows;
			expandedRows            = expandedExpressionRows;
			return true;
		}

		List<(Expression path, ColumnDescriptor? descriptor, SqlPlaceholderExpression placeholder)> _fieldsMap = new ();

		SqlPlaceholderExpression? BuildFieldPlaceholder(MemberExpression? memberExpression, SqlPathExpression? pathExpression, ProjectFlags flags)
		{
			if (memberExpression == null && pathExpression == null)
				return null;

			var currentDescriptor = Builder.CurrentDescriptor;
			SqlPlaceholderExpression? foundPlaceholder = null;

			foreach (var (path, descriptor, placeholder) in _fieldsMap)
			{
				if (descriptor == currentDescriptor && (ExpressionEqualityComparer.Instance.Equals(path, memberExpression) || ExpressionEqualityComparer.Instance.Equals(path, pathExpression)))
				{
					foundPlaceholder = placeholder;
					break;
				}
			}

			if (foundPlaceholder != null)
			{
				return foundPlaceholder;
			}

			var  translations = new List<Expression>();

			if (memberExpression != null)
			{
				var isSpecial = SequenceHelper.IsSpecialProperty(memberExpression, memberExpression.Type, "item");
				var isRowIndex = SequenceHelper.IsSpecialProperty(memberExpression, memberExpression.Type, "index");

				for (var index = 0; index < _expressionRows.Length; index++)
				{
					var rowProjection = _expressionRows[index];
					var projected = isSpecial ? rowProjection
						: isRowIndex ? ExpressionInstances.Int32(index)
						: Builder.Project(Parent!, memberExpression, null, 0, flags, rowProjection, false);
					var translated = Builder.BuildSqlExpression(Parent, projected);
					translations.Add(translated);
				}
			}
			else if (pathExpression != null)
			{
				if (!ByPathExpressions.TryGetValue(pathExpression, out var list))
				{
					return null;
				}

				for (var i = 0; i < _expressionRows.Length; i++)
				{
					var foundIndex = list.FindIndex(tuple => tuple.rowIndex == i);
					if (foundIndex >= 0)
					{
						translations.Add(list[foundIndex].Item2);
					}
					else
					{
						translations.Add(_expressionRows[i]);
					}
				}
			}

			var firstTranslated = translations.OfType<SqlPlaceholderExpression>().FirstOrDefault();

			if (firstTranslated == null)
			{
				return null;
			}

			Expression currentPath = memberExpression != null ? memberExpression : pathExpression!;

			var fieldName = GenerateFieldName(currentPath);
			if (fieldName == null)
			{
				fieldName = "field1";
			}

			Utils.MakeUniqueNames([fieldName], _fieldsMap.Select(x => ((SqlField)x.placeholder.Sql).Name), x => x, (e, v, s) => fieldName = v);

			var dbDataType = currentDescriptor?.GetDbDataType(true) ?? QueryHelper.GetDbDataType(firstTranslated.Sql, MappingSchema);

			var field = new SqlField(dbDataType, fieldName, true);
			var fieldPlaceholder = ExpressionBuilder.CreatePlaceholder(this, field, currentPath);

			_fieldsMap.Add((currentPath, currentDescriptor, fieldPlaceholder));

			Table.AddField(field);

			// fill rows
			if (Table.Rows == null)
			{
				Table.Rows = new ();
				Table.Rows.AddRange(Enumerable.Range(0, translations.Count).Select(_ => new List<ISqlExpression>()));
			}

			for (var i = 0; i < translations.Count; i++)
			{
				var translated = translations[i];

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
