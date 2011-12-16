using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Data.Linq
{
	using SqlBuilder;

	[SerializableAttribute]
	[AttributeUsageAttribute(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class TableExpressionAttribute : TableFunctionAttribute
	{
		public TableExpressionAttribute(string expression)
			: base(expression)
		{
		}

		public TableExpressionAttribute(string expression, params int[] argIndices)
			: base(expression, argIndices)
		{
		}

		public TableExpressionAttribute(string sqlProvider, string expression)
			: base(sqlProvider, expression)
		{
		}

		public TableExpressionAttribute(string sqlProvider, string expression, params int[] argIndices)
			: base(sqlProvider, expression, argIndices)
		{
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

		public override void SetTable(SqlTable table, MemberInfo member, IEnumerable<Expression> arguments, IEnumerable<ISqlExpression> sqlArgs)
		{
			table.SqlTableType   = SqlTableType.Expression;
			table.Name           = Expression ?? member.Name;
			table.TableArguments = ConvertArgs(member, sqlArgs.ToArray());
		}
	}
}
