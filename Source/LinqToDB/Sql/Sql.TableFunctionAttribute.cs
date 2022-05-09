using System;
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
		public class TableFunctionAttribute : Attribute
		{
			public TableFunctionAttribute()
			{
			}

			public TableFunctionAttribute(string name)
			{
				Name = name;
			}

			public TableFunctionAttribute(string name, params int[] argIndices)
			{
				Name        = name;
				ArgIndices  = argIndices;
			}

			public TableFunctionAttribute(string configuration, string name)
			{
				Configuration = configuration;
				Name          = name;
			}

			public TableFunctionAttribute(string configuration, string name, params int[] argIndices)
			{
				Configuration = configuration;
				Name          = name;
				ArgIndices    = argIndices;
			}

			public string? Configuration { get; set; }
			public string? Name          { get; set; }
			public string? Schema        { get; set; }
			public string? Database      { get; set; }
			public string? Server        { get; set; }
			public string? Package       { get; set; }
			public int[]?  ArgIndices    { get; set; }

			public virtual void SetTable<TContext>(TContext context, ISqlBuilder sqlBuilder, MappingSchema mappingSchema, SqlTable table, MethodCallExpression methodCall, Func<TContext, Expression, ColumnDescriptor?, ISqlExpression> converter)
			{
				table.SqlTableType   = SqlTableType.Function;
				table.Name           = Name ?? methodCall.Method.Name;
				table.TableName      = table.TableName with { Name = Name ?? methodCall.Method.Name };

				var expressionStr = table.Name;
				ExpressionAttribute.PrepareParameterValues(methodCall, ref expressionStr, false, out var knownExpressions, false, out var genericTypes);

				if (string.IsNullOrEmpty(expressionStr))
					throw new LinqToDBException($"Cannot retrieve Table Function body from expression '{methodCall}'.");

				table.TableArguments = ExpressionAttribute.PrepareArguments(context, string.Empty, ArgIndices, true, knownExpressions, genericTypes, converter);

				if (Schema   != null) table.TableName = table.TableName with { Schema   = Schema };
				if (Database != null) table.TableName = table.TableName with { Database = Database };
				if (Server   != null) table.TableName = table.TableName with { Server   = Server };
				if (Package  != null) table.TableName = table.TableName with { Package  = Package };
			}
		}
	}
}
