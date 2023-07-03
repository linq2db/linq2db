using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
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
			if (sqlExpression is SqlField field)
				return field.PhysicalName;

			if (sqlExpression is SqlColumn column)
				return column.RawAlias ?? GenerateColumnAlias(column.Expression);

			return null;
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

			while (fields.Count < index + 1)
				fields.Add(null!);

			fields[index] = newField;

			return newField;
		}

		public static Expression RemapToFields(
			IBuildContext                                                   subQueryContext, 
			ISqlTableSource?                                                parentTable, 
			List<SqlField>                                                  fields, 
			Dictionary<Expression, SqlPlaceholderExpression>                knownMap, 
			Expression                                                      expression, 
			List<(SqlPlaceholderExpression placeholder, MemberInfo[] path)> placeholders)
		{
			if (placeholders.Count == 0)
				return expression;

			var needsTransformation = false;

			var newPlaceholders = new SqlPlaceholderExpression[placeholders.Count];

			for (var index = 0; index < placeholders.Count; index++)
			{
				var (placeholder, path) = placeholders[index];

				if (placeholder.TrackingPath == null)
					continue;

				if (!knownMap.TryGetValue(placeholder.TrackingPath, out var newPlaceholder))
				{
					// We change path to MakeColumn's cache always create columns for such tables
					//
					var updatedPlaceholder = placeholder.WithPath(placeholder.TrackingPath);

					updatedPlaceholder = (SqlPlaceholderExpression)subQueryContext.Builder.UpdateNesting(subQueryContext, updatedPlaceholder);

					var field = RegisterFieldMapping(fields, updatedPlaceholder.Index!.Value, () =>
					{
						var alias = (path.Length > 0 ? GenerateColumnAlias(path) : GenerateColumnAlias(updatedPlaceholder.Path)) ?? GenerateColumnAlias(updatedPlaceholder.Sql);
						var dataType = QueryHelper.GetDbDataType(updatedPlaceholder.Sql);
						var newField = new SqlField(dataType, alias, updatedPlaceholder.Sql.CanBeNullable(NullabilityContext.NonQuery));

						newField.Table = parentTable;
						return newField;
					});

					newPlaceholder = ExpressionBuilder.CreatePlaceholder(subQueryContext!.SelectQuery, field,
						updatedPlaceholder.Path, trackingPath: updatedPlaceholder.TrackingPath, index: updatedPlaceholder.Index);

					knownMap[updatedPlaceholder.TrackingPath!] = newPlaceholder;
				}
				else
				{
					if (newPlaceholder.Sql is SqlField sqlField && newPlaceholder.SelectQuery == null)
					{
						var idx = placeholder.Index.Value;
						while (idx >= fields.Count)
							fields.Add(null!);

						fields[idx]       = sqlField;
						//knownMap[placeholder.TrackingPath] = newPlaceholder;
					}
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
					var index = ctx.placeholders.FindIndex(pi => pi.placeholder == placeholder);
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
