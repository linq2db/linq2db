using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.SqlQuery
{
	public interface IReadOnlyParameterValues
	{
		bool TryGetValue(SqlParameter parameter, [NotNullWhen(true)] out SqlParameterValue? value);
	}
}
