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
		public class TableFunctionAttribute : Attribute, IConfigurationProvider
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

			public string? Configuration { get; init; }
			public string? Name          { get; init; }
			public string? Schema        { get; init; }
			public string? Database      { get; init; }
			public string? Server        { get; init; }
			public int[]?  ArgIndices    { get; init; }

			public virtual void SetTable<TContext>(TContext context, ISqlBuilder sqlBuilder, MappingSchema mappingSchema, SqlTable table, MethodCallExpression methodCall, Func<TContext, Expression, ColumnDescriptor?, ISqlExpression> converter)
			{
				table.SqlTableType   = SqlTableType.Function;
				table.Name           = Name ?? methodCall.Method.Name;
				table.PhysicalName   = Name ?? methodCall.Method.Name;

				var expressionStr = table.Name;
				ExpressionAttribute.PrepareParameterValues(methodCall, ref expressionStr, false, out var knownExpressions, out var genericTypes);

				if (string.IsNullOrEmpty(expressionStr))
					throw new LinqToDBException($"Cannot retrieve Table Function body from expression '{methodCall}'.");

				table.TableArguments = ExpressionAttribute.PrepareArguments(context, string.Empty, ArgIndices, addDefault: true, knownExpressions, genericTypes, converter);

				if (Schema   != null) table.Schema   = Schema;
				if (Database != null) table.Database = Database;
				if (Server   != null) table.Server   = Server;
			}
		}
	}
}
