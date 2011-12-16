using System;
using System.Reflection;

using LinqToDB.Extensions;
using LinqToDB.Sql;

namespace LinqToDB.Data.Linq
{
	[SerializableAttribute]
	[AttributeUsageAttribute(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class SqlExpressionAttribute : SqlFunctionAttribute
	{
		public SqlExpressionAttribute(string expression)
			: base(expression)
		{
			Precedence = LinqToDB.Sql.Precedence.Primary;
		}

		public SqlExpressionAttribute(string expression, params int[] argIndices)
			: base(expression, argIndices)
		{
			Precedence = LinqToDB.Sql.Precedence.Primary;
		}

		public SqlExpressionAttribute(string sqlProvider, string expression)
			: base(sqlProvider, expression)
		{
			Precedence = LinqToDB.Sql.Precedence.Primary;
		}

		public SqlExpressionAttribute(string sqlProvider, string expression, params int[] argIndices)
			: base(sqlProvider, expression, argIndices)
		{
			Precedence = LinqToDB.Sql.Precedence.Primary;
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
