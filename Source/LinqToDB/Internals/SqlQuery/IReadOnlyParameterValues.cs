using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internals.SqlQuery
{
	public interface IReadOnlyParameterValues
	{
		bool TryGetValue(SqlParameter parameter, [NotNullWhen(true)] out SqlParameterValue? value);
	}
}
