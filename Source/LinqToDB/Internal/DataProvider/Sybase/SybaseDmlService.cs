using System;

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Sybase
{
	/// <summary>
	/// Sybase ASE DML mechanics. Truncate-with-identity-reset is split into the truncate plus an
	/// <see cref="SqlCommandFragment.IdentityReseed"/> fragment (<c>sp_chgattribute ... 'identity_burn_max'</c>);
	/// everything else (including identity inserts, which emit their own trailing select) uses the legacy path.
	/// </summary>
	public sealed class SybaseDmlService : DmlServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception) => false;

		public override SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			if (statement is SqlTruncateTableStatement { ResetIdentity: true } truncate && truncate.Table!.IdentityFields.Count > 0)
			{
				return new SqlCommandScenario
				{
					Steps =
					[
						new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery },
						new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery, Fragment = SqlCommandFragment.IdentityReseed },
					],
					OutcomeSteps = [0],
				};
			}

			return null;
		}
	}
}
