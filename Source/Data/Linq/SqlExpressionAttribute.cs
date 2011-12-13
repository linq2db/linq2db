using System;
using System.Reflection;

namespace LinqToDB.Data.Linq
{
	using Data.Sql;
	using Reflection;

	[SerializableAttribute]
	[AttributeUsageAttribute(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class SqlExpressionAttribute : SqlFunctionAttribute
	{
		public SqlExpressionAttribute(string expression)
			: base(expression)
		{
			Precedence = Data.Sql.Precedence.Primary;
		}

		public SqlExpressionAttribute(string expression, params int[] argIndices)
			: base(expression, argIndices)
		{
			Precedence = Data.Sql.Precedence.Primary;
		}

		public SqlExpressionAttribute(string sqlProvider, string expression)
			: base(sqlProvider, expression)
		{
			Precedence = Data.Sql.Precedence.Primary;
		}

		public SqlExpressionAttribute(string sqlProvider, string expression, params int[] argIndices)
			: base(sqlProvider, expression, argIndices)
		{
			Precedence = Data.Sql.Precedence.Primary;
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
			return new SqlExpression(TypeHelper.GetMemberType(member), Expression ?? member.Name, Precedence, ConvertArgs(member, args));
		}
	}
}
