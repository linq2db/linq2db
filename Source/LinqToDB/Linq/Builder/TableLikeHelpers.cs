using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Common;
	using LinqToDB.Expressions;
	using LinqToDB.Extensions;
	using LinqToDB.SqlQuery;

	static class TableLikeHelpers
	{
		public static List<MemberInfo> GetMemberPath(Expression expr)
		{
			var result  = new List<MemberInfo>();
			var current = expr;

			while (current is MemberExpression memberExpression)
			{
				result.Insert(0, memberExpression.Member);
				current = memberExpression.Expression;
			}

			return result;
		}

		public static string? GenerateColumnAlias(ISqlExpression sqlExpression)
		{
			return sqlExpression switch
			{
				SqlField field   => field.PhysicalName,
				SqlColumn column => column.RawAlias ?? GenerateColumnAlias(column.Expression),
				_                => null,
			};
		}

		public static string? GenerateColumnAlias(Expression expr)
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

		public static string? GenerateColumnAlias(MemberInfo[] path)
		{
			string? alias = null;

			foreach (var current in path)
			{
				if (alias != null)
					alias = current.Name + "_" + alias;
				else
					alias = current.Name;
			}

			return alias;
		}

		public static SqlField RegisterFieldMapping(List<SqlField> fields, int index, Func<SqlField> fieldFactory)
		{
			/*
			if (fields.Count > index && fields[index] != null)
				return fields[index];
				*/

			var newField = fieldFactory();

			Utils.MakeUniqueNames(new[] { newField }, fields.Where(f => f != null).Select(t => t.Name), f => f.Name, (f, n, a) =>
			{
				f.Name         = n;
				f.PhysicalName = n;
			}, f => (string.IsNullOrEmpty(f.Name) ? "field" : f.Name) + "_1");

			if (index >= fields.Count)
				fields.Add(newField);
			else
				fields.Insert(index, newField);

			return newField;
		}

		public static Expression RemapToFields(
			IBuildContext                                     subQueryContext,
			ISqlTableSource?                                  parentTable,
			List<SqlField>                                    fields,
			Dictionary<Expression, SqlPlaceholderExpression>  knownMap,
			Dictionary<Expression, SqlPlaceholderExpression>? recursiveMap,
			Expression                                        expression,
			List<SqlPlaceholderExpression>                    placeholders)
		{
			if (placeholders.Count == 0)
				return expression;

			var needsTransformation = false;

			var newPlaceholders = new SqlPlaceholderExpression[placeholders.Count];

			for (var index = 0; index < placeholders.Count; index++)
			{
				var placeholder = placeholders[index];

				if (placeholder.TrackingPath == null)
					continue;

				if (!knownMap.TryGetValue(placeholder.TrackingPath, out var newPlaceholder))
				{
					// We change path to MakeColumn's cache always create columns for such tables
					//
					var updatedPlaceholder = placeholder.WithPath(placeholder.TrackingPath);

					updatedPlaceholder = subQueryContext.Builder.UpdateNesting(subQueryContext, updatedPlaceholder);

					var nullabilityContext = NullabilityContext.GetContext(subQueryContext.SelectQuery);

					var placeholderIndex = updatedPlaceholder.Index!.Value;
					var field = RegisterFieldMapping(fields, placeholderIndex, () =>
					{
						var alias = GenerateColumnAlias(updatedPlaceholder.Path) ?? GenerateColumnAlias(updatedPlaceholder.Sql);
						var dataType = QueryHelper.GetDbDataType(updatedPlaceholder.Sql, subQueryContext.MappingSchema);

						SqlField newField;
						var      isNullable = updatedPlaceholder.Sql.CanBeNullable(nullabilityContext);

						if (recursiveMap != null && recursiveMap.TryGetValue(placeholder.TrackingPath, out var recursiveField))
						{
							newField = (SqlField)recursiveField.Sql;
							if (isNullable != newField.CanBeNull)
							{
								newField = new SqlField(newField.Type, newField.Name, isNullable);
							}
						}
						else
						{
							newField = new SqlField(dataType, alias, isNullable);
						}

						newField.Table = parentTable;
						return newField;
					});

					newPlaceholder = ExpressionBuilder.CreatePlaceholder(subQueryContext!.SelectQuery, field,
						updatedPlaceholder.Path, trackingPath: updatedPlaceholder.TrackingPath, index: updatedPlaceholder.Index);

					knownMap[updatedPlaceholder.TrackingPath!] = newPlaceholder;
				}

				if (!ReferenceEquals(newPlaceholder, placeholder))
					needsTransformation = true;

				if (placeholder.Type != newPlaceholder.Type)
				{
					if (placeholder.Type.IsNullable())
					{
						if (!newPlaceholder.Type.IsNullable())
							newPlaceholder = newPlaceholder.MakeNullable();
					}
					else
					{
						if (newPlaceholder.Type.IsNullable())
							newPlaceholder = newPlaceholder.MakeNotNullable();

					}
				}

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

	}
}
