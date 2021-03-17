using System;
using System.Reflection;

using JetBrains.Annotations;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	using Extensions;
	using SqlQuery;

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

			public override ISqlExpression GetExpression(MemberInfo member, params ISqlExpression[] args)
			{
				var sqlExpressions = ConvertArgs(member, args);

				return new SqlFunction(member.GetMemberType(), Name ?? member.Name, IsAggregate, IsPure, sqlExpressions)
				{
					CanBeNull = GetCanBeNull(sqlExpressions)
				};
			}
		}
	}
}
