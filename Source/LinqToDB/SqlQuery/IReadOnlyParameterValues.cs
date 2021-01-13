using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlQuery
{
	public interface IReadOnlyParameterValues
	{
		bool TryGetValue(SqlParameter parameter, [NotNullWhen(true)] out SqlParameterValue? value);
	}
}
