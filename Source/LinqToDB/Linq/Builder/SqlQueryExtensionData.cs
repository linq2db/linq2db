using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder;

using SqlQuery;

public class SqlQueryExtensionData
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
}
