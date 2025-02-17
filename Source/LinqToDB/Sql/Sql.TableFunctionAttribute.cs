using System;
using System.Linq.Expressions;

using LinqToDB.Common.Internal;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	partial class Sql
	{
		[Serializable]
		[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class TableFunctionAttribute : MappingAttribute
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

			public string? Name          { get; set; }
			public string? Schema        { get; set; }
			public string? Database      { get; set; }
			public string? Server        { get; set; }
			public string? Package       { get; set; }
			public int[]?  ArgIndices    { get; set; }

			public virtual void SetTable<TContext>(DataOptions options, TContext context, ISqlBuilder sqlBuilder, MappingSchema mappingSchema, SqlTable table, MethodCallExpression methodCall, ExpressionAttribute.ConvertFunc<TContext> converter)
			{
				table.SqlTableType = SqlTableType.Function;
				var expressionStr  = table.Expression = Name ?? methodCall.Method.Name!;

				ExpressionAttribute.PrepareParameterValues(context, mappingSchema, methodCall, ref expressionStr, false, out var knownExpressions, false, false, out var genericTypes, converter);

				if (string.IsNullOrEmpty(expressionStr))
					throw new LinqToDBException($"Cannot retrieve Table Function body from expression '{methodCall}'.");

				table.TableName = new SqlObjectName(
					expressionStr!,
					Schema  : Schema   ?? table.TableName.Schema,
					Database: Database ?? table.TableName.Database,
					Server  : Server   ?? table.TableName.Server,
					Package : Package  ?? table.TableName.Package);

				table.TableArguments = ExpressionAttribute.PrepareArguments(context, string.Empty, ArgIndices, true, knownExpressions, genericTypes, converter, false, out var error)!;

				if (error != null)
					throw Expressions.SqlErrorExpression.EnsureError(error).CreateException();
			}

			public override string GetObjectID()
			{
				return $".{Configuration}.{Name}.{Schema}.{Database}.{Server}.{Package}.{IdentifierBuilder.GetObjectID(ArgIndices)}.";
			}
		}
	}
}
