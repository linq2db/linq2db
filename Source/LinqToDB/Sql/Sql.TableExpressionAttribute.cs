﻿using System;
using System.Linq;
using System.Linq.Expressions;
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

			public override void SetTable<TContext>(TContext context, ISqlBuilder sqlBuilder, MappingSchema mappingSchema, SqlTable table, MethodCallExpression methodCall, Func<TContext, Expression, ColumnDescriptor?, ISqlExpression> converter)
			{
				table.SqlTableType = SqlTableType.Expression;
				var expressionStr  = table.Expression = Expression ?? methodCall.Method.Name!;

				ExpressionAttribute.PrepareParameterValues(context, mappingSchema, methodCall, ref expressionStr, false, out var knownExpressions, false, out var genericTypes, converter);

				if (string.IsNullOrEmpty(expressionStr))
					ThrowHelper.ThrowLinqToDBException($"Cannot retrieve Table Expression body from expression '{methodCall}'.");

				// Add two fake expressions, TableName and Alias
				knownExpressions.Insert(0, null);
				knownExpressions.Insert(0, null);

				if (Schema != null || Database != null || Server != null || Package != null)
					table.TableName = new SqlObjectName(
						table.TableName.Name,
						Schema  : Schema   ?? table.TableName.Schema,
						Database: Database ?? table.TableName.Database,
						Server  : Server   ?? table.TableName.Server,
						Package : Package  ?? table.TableName.Package);

				table.TableArguments = ExpressionAttribute.PrepareArguments(context, expressionStr!, ArgIndices, false, knownExpressions, genericTypes, converter).Skip(2).ToArray();
			}
		}
	}
}
