using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public class SqlApplyTableExpression
	{
		public SqlApplyTableExpression(bool isExcept, string expressionStr, IEnumerable<string> groups)
		{
			IsExcept = isExcept;
			ExpressionStr = expressionStr;
			Groups = new HashSet<string>(groups);
		}

		public bool IsExcept { get; }
		public string ExpressionStr { get; }
		public HashSet<string> Groups { get; }
	}
}
