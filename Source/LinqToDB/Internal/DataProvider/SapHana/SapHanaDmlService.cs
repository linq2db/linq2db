using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	/// <summary>
	/// SAP HANA DML mechanics. An identity insert is split into the insert (non-query) plus a scalar
	/// <c>SELECT CURRENT_IDENTITY_VALUE()</c> (the builder appends <c>FROM DUMMY</c> for the from-less select);
	/// everything else falls back to the legacy command-splitting path.
	/// </summary>
	public sealed class SapHanaDmlService : DmlServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception) => false;

		public override SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			if (statement.NeedsIdentity)
			{
				// CURRENT_IDENTITY_VALUE() returns the last identity generated in the SESSION, so without this guard an
				// insert into a table carrying no identity column would silently return a value produced by an unrelated
				// earlier insert (or NULL on a fresh session). The removed SapHanaSqlBuilder.BuildCommand carried the
				// same check, and every sibling provider still does.
				var into = statement.InsertClause!.Into;

				if (into?.GetIdentityField() == null)
					throw new LinqToDBException($"Identity field must be defined for '{into?.NameForLogging}'.");

				var idType = factory.GetDbDataType(typeof(long));

				return IdentitySelectScenario(statement, factory.Function(idType, "CURRENT_IDENTITY_VALUE"));
			}

			return base.BuildCommandScenario(statement, flags, factory);
		}
	}
}
