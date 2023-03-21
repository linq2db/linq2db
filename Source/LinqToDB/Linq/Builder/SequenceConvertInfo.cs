using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	public class SequenceConvertInfo
	{
		public ParameterExpression?       Parameter;
		public Expression                 Expression = null!;
		public List<SequenceConvertPath>? ExpressionsToReplace;
	}
}
