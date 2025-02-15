using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	[DebuggerDisplay("{ToDebugString()}")]
	public sealed class SqlQueryExtensionData
	{
		public SqlQueryExtensionData(string name, Expression expr, ParameterInfo parameter, int paramsIndex = -1)
		{
			Name        = name;
			Expression  = expr;
			Parameter   = parameter;
			ParamsIndex = paramsIndex;
		}

		public string          Name          { get; }
		public Expression      Expression    { get; }
		public ParameterInfo   Parameter     { get; }
		public int             ParamsIndex   { get; }
		public ISqlExpression? SqlExpression { get; set; }

		public string ToDebugString()
		{
			return $"{Name} = {SqlExpression?.ToDebugString() ?? "Expr:" + Expression.ToString()}";
		}
	}
}
