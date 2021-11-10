using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	public class SqlQueryExtensionData
	{
		public string          Name          { get; set; }
		public ParameterInfo?  Parameter     { get; set; }
		public Expression?     Expression    { get; set; }
		public ISqlExpression? SqlExpression { get; set; }
	}
}
