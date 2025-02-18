using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Builder;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	partial class Sql
	{
		/// <summary>
		/// Defines an SQL server-side Function with parameters passed in.
		/// </summary>
		[PublicAPI]
		[Serializable]
		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
		public class FunctionAttribute : ExpressionAttribute
		{
			/// <summary>
			/// Defines an SQL Function, which
			/// shall be the same as the name as the function called.
			/// </summary>
			public FunctionAttribute()
				: base(null)
			{
				IsNullable = IsNullableType.IfAnyParameterNullable;
			}

			/// <summary>
			/// Defines an SQL function with the given name.
			/// </summary>
			/// <param name="name">The name of the function. no parenthesis () should be used.</param>
			public FunctionAttribute(string name)
				: base(name)
			{
				IsNullable = IsNullableType.IfAnyParameterNullable;
			}

			/// <summary>
			/// Defines an SQL function with the given name.
			/// </summary>
			/// <param name="name">The name of the function. no parenthesis () should be used.</param>
			/// <param name="argIndices">Used for setting the order of the method arguments
			/// being passed into the function.</param>
			public FunctionAttribute(string name, params int[] argIndices)
				: base(name, argIndices)
			{
				IsNullable = IsNullableType.IfAnyParameterNullable;
			}

			/// <summary>
			/// Defines an SQL function with the given name,
			/// for the <see cref="ProviderName"/> given.
			/// </summary>
			/// <param name="configuration">The Database configuration for which this Expression will be used.</param>
			/// <param name="name">The name of the function. no parenthesis () should be used.</param>
			public FunctionAttribute(string configuration, string name)
				: base(configuration, name)
			{
				IsNullable = IsNullableType.IfAnyParameterNullable;
			}

			/// <summary>
			/// Defines an SQL function with the given name,
			/// for the <see cref="ProviderName"/> given.
			/// </summary>
			/// <param name="configuration">The Database configuration for which this Expression will be used.</param>
			/// <param name="name">The name of the function. no parenthesis () should be used.</param>
			/// <param name="argIndices">Used for setting the order of the method arguments
			/// being passed into the function.</param>
			public FunctionAttribute(string configuration, string name, params int[] argIndices)
				: base(configuration, name, argIndices)
			{
				IsNullable = IsNullableType.IfAnyParameterNullable;
			}

			/// <summary>
			/// The name of the Database Function
			/// </summary>
			public string? Name
			{
				get => Expression;
				set => Expression = value;
			}

			public override Expression GetExpression<TContext>(
				TContext              context,
				IDataContext          dataContext,
				IExpressionEvaluator  evaluator,
				SelectQuery           query,
				Expression            expression,
				ConvertFunc<TContext> converter)
			{
				var expressionStr = Expression;
				PrepareParameterValues(context, dataContext.MappingSchema, expression, ref expressionStr, true, out var knownExpressions, IgnoreGenericParameters, InlineParameters, out var genericTypes, converter);

				if (string.IsNullOrEmpty(expressionStr))
					throw new LinqToDBException($"Cannot retrieve function name for expression '{expression}'.");

				var parameters = PrepareArguments(context, expressionStr!, ArgIndices, addDefault: true, knownExpressions, genericTypes, converter, InlineParameters, out var error);

				if (error != null)
					return SqlErrorExpression.EnsureError(error, expression.Type);

				var function = new SqlFunction(expression.Type, expressionStr!, IsAggregate, IsPure, Precedence,
					ToParametersNullabilityType(IsNullable), _canBeNull, parameters!);

				return ExpressionBuilder.CreatePlaceholder(query, function, expression);
			}

			public override string GetObjectID()
			{
				return $"{base.GetObjectID()}.{Name}.";
			}
		}
	}
}
