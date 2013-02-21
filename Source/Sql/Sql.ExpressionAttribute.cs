using System;
using System.Reflection;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	using Extensions;
	using SqlBuilder;

	partial class Sql
	{
		[Serializable]
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class ExpressionAttribute : FunctionAttribute
		{
			public ExpressionAttribute(string expression)
				: base(expression)
			{
				Precedence = SqlBuilder.Precedence.Primary;
			}

			public ExpressionAttribute(string expression, params int[] argIndices)
				: base(expression, argIndices)
			{
				Precedence = SqlBuilder.Precedence.Primary;
			}

			public ExpressionAttribute(string sqlProvider, string expression)
				: base(sqlProvider, expression)
			{
				Precedence = SqlBuilder.Precedence.Primary;
			}

			public ExpressionAttribute(string sqlProvider, string expression, params int[] argIndices)
				: base(sqlProvider, expression, argIndices)
			{
				Precedence = SqlBuilder.Precedence.Primary;
			}

			protected new string Name
			{
				get { return base.Name; }
			}

			public string Expression
			{
				get { return base.Name;  }
				set { base.Name = value; }
			}

			public int Precedence { get; set; }

			public override ISqlExpression GetExpression(MemberInfo member, params ISqlExpression[] args)
			{
				return new SqlExpression(member.GetMemberType(), Expression ?? member.Name, Precedence, ConvertArgs(member, args));
			}
		}
	}
}
