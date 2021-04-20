using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Mapping;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	using LinqToDB.SqlProvider;
	using SqlQuery;

	partial class Sql
	{
		[Serializable]
		[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class TableExpressionAttribute : TableFunctionAttribute
		{
			public TableExpressionAttribute(string expression)
				: base(expression)
			{
			}

			public TableExpressionAttribute(string expression, params int[] argIndices)
				: base(expression, argIndices)
			{
			}

			public TableExpressionAttribute(string sqlProvider, string expression)
				: base(sqlProvider, expression)
			{
			}

			public TableExpressionAttribute(string sqlProvider, string expression, params int[] argIndices)
				: base(sqlProvider, expression, argIndices)
			{
			}

			protected new string? Name => base.Name;

			public string? Expression
			{
				get => base.Name;
				set => base.Name = value;
			}

			public override void SetTable(ISqlBuilder sqlBuilder, MappingSchema mappingSchema, SqlTable table, MethodCallExpression methodCall, Func<Expression, ColumnDescriptor?, ISqlExpression> converter)
			{
				table.SqlTableType   = SqlTableType.Expression;
				table.Name           = Expression ?? methodCall.Method.Name;

				var expressionStr = table.Name;
				ExpressionAttribute.PrepareParameterValues(methodCall, ref expressionStr, false, out var knownExpressions, out var genericTypes);

				if (string.IsNullOrEmpty(expressionStr))
					throw new LinqToDBException($"Cannot retrieve Table Expression body from expression '{methodCall}'.");

				// Add two fake expressions, TableName and Alias
				knownExpressions.Insert(0, null);
				knownExpressions.Insert(0, null);

				table.TableArguments = ExpressionAttribute.PrepareArguments(expressionStr!, ArgIndices, false, knownExpressions, genericTypes, converter).Skip(2).ToArray();

				if (Schema   != null) table.Schema   = Schema;
				if (Database != null) table.Database = Database;
				if (Server   != null) table.Server   = Server;
			}
		}
	}
}
