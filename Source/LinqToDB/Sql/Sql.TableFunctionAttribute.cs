using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Mapping;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	using Extensions;
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
			public int[]?  ArgIndices    { get; set; }

			public virtual void SetTable(ISqlBuilder sqlBuilder, MappingSchema mappingSchema, SqlTable table, MethodCallExpression methodCall, Func<Expression, ColumnDescriptor?, ISqlExpression> converter)
			{
				table.SqlTableType   = SqlTableType.Function;
				table.Name           = Name ?? methodCall.Method.Name;
				table.PhysicalName   = Name ?? methodCall.Method.Name;

				var expressionStr = table.Name;
				ExpressionAttribute.PrepareParameterValues(methodCall, ref expressionStr, false, out var knownExpressions, out var genericTypes);

				if (string.IsNullOrEmpty(expressionStr))
					throw new LinqToDBException($"Cannot retrieve Table Function body from expression '{methodCall}'.");

				// Add two fake expressions, TableName and Alias
				knownExpressions.Insert(0, null);
				knownExpressions.Insert(0, null);

				table.TableArguments = ExpressionAttribute.PrepareArguments(expressionStr!, ArgIndices, knownExpressions, genericTypes, converter).Skip(2).ToArray();

				if (Schema   != null) table.Schema   = Schema;
				if (Database != null) table.Database = Database;
				if (Server   != null) table.Server   = Server;
			}
		}
	}
}
