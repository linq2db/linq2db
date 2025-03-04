using LinqToDB.Internal.Linq.Builder.Visitors;

namespace LinqToDB.EntityFrameworkCore.Internal
{
	/// <inheritdoc />
	public class CanBeValuatedVisitor : CanBeEvaluatedOnClientCheckVisitorBase
	{
		/// <summary>
		/// Result of expression analysis.
		/// </summary>
		public new bool CanBeEvaluated => base.CanBeEvaluated;
	}
}
