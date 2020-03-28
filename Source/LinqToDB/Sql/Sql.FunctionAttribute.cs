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
		[PublicAPI]
		[Serializable]
		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
		public class FunctionAttribute : ExpressionAttribute
		{
			public FunctionAttribute()
				: base(null)
			{
				IsNullable = IsNullableType.IfAnyParameterNullable;
			}

			public FunctionAttribute(string name)
				: base(name)
			{
				IsNullable = IsNullableType.IfAnyParameterNullable;
			}

			public FunctionAttribute(string name, params int[] argIndices)
				: base(name, argIndices)
			{
				IsNullable = IsNullableType.IfAnyParameterNullable;
			}

			public FunctionAttribute(string configuration, string name)
				: base(configuration, name)
			{
				IsNullable = IsNullableType.IfAnyParameterNullable;
			}

			public FunctionAttribute(string configuration, string name, params int[] argIndices)
				: base(configuration, name, argIndices)
			{
				IsNullable = IsNullableType.IfAnyParameterNullable;
			}

			public string? Name
			{
				get => Expression;
				set => Expression = value;
			}

			public override ISqlExpression GetExpression(MemberInfo member, params ISqlExpression[] args)
			{
				var sqlExpressions = ConvertArgs(member, args);

				return new SqlFunction(member.GetMemberType(), Name ?? member.Name, IsAggregate, sqlExpressions)
				{
					CanBeNull = GetCanBeNull(sqlExpressions)
				};
			}
		}
	}
}
