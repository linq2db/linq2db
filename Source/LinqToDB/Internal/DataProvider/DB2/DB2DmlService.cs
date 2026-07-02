using System;

using LinqToDB.DataProvider.DB2;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.DB2
{
	/// <summary>
	/// DB2 DML mechanics. Identity retrieval is version-specific: LUW wraps a with-identity insert as
	/// <c>SELECT &lt;id&gt; FROM NEW TABLE (INSERT …)</c> (one scalar command, emitted by the builder), or — when no
	/// identity field is detected — falls back to the insert plus <c>SELECT identity_val_local()</c>; z/OS appends
	/// <c>; SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1</c> to the insert (builder <c>BuildGetIdentity</c>).
	/// A truncate-with-reset becomes the truncate plus one <see cref="SqlCommandFragment.IdentityReseed"/> fragment per
	/// identity column (<c>ALTER TABLE … ALTER … RESTART WITH 1</c>). Everything else uses the base scenario.
	/// </summary>
	public abstract class DB2DmlService : DmlServiceBase
	{
		// Version is carried by the concrete subclass (DB2LUWDmlService / DB2zOSDmlService) rather than a constructor
		// argument, so the remote client can reconstruct the service from its type name via a parameterless Activator.
		protected abstract DB2Version Version { get; }

		// DB2 had no dedicated table-not-found detection before this DML service existed (it used the default), so keep
		// that behavior for now. A proper SQLSTATE 42704 / SQL0204N check could be added as a follow-up.
		protected override bool IsTableNotFoundExceptionCore(Exception exception) => false;

		public override SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			if (statement is SqlTruncateTableStatement { ResetIdentity: true } truncate && truncate.Table!.IdentityFields.Count > 0)
			{
				var fields = truncate.Table.IdentityFields;
				var steps  = new SqlCommandStep[fields.Count + 1];

				steps[0] = new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery };

				for (var i = 0; i < fields.Count; i++)
					steps[i + 1] = new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery, Fragment = SqlCommandFragment.IdentityReseed, FragmentFieldIndex = i };

				return new SqlCommandScenario { Steps = steps, OutcomeSteps = [0] };
			}

			if (statement is SqlInsertStatement { Insert.WithIdentity: true } insert)
			{
				// LUW without a detectable identity field: run the insert, then SELECT identity_val_local() (the builder
				// renders the from-less SELECT with FROM SYSIBM.SYSDUMMY1).
				if (Version == DB2Version.LUW && insert.Insert.Into!.GetIdentityField() == null)
				{
					var idType   = factory.GetDbDataType(typeof(long));
					var idSelect = new SqlSelectStatement();

					idSelect.SelectQuery.Select.AddNew(factory.Function(idType, "identity_val_local"));

					return new SqlCommandScenario
					{
						Steps =
						[
							new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery },
							new SqlCommandStep { Statement = idSelect,  Kind = SqlStepKind.Scalar   },
						],
						OutcomeSteps = [1],
					};
				}

				// LUW (identity field): the builder wraps the insert as SELECT <id> FROM NEW TABLE (INSERT ...).
				// z/OS: the builder appends "; SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1". Either way the insert
				// renders as a single scalar command returning the identity.
				return new SqlCommandScenario
				{
					Steps        = [new SqlCommandStep { Statement = statement, Kind = SqlStepKind.Scalar }],
					OutcomeSteps = [0],
				};
			}

			return base.BuildCommandScenario(statement, flags, factory);
		}
	}

	/// <summary>DB2 LUW DML mechanics. Parameterless so the remote client can reconstruct it from its type name.</summary>
	public sealed class DB2LUWDmlService : DB2DmlService
	{
		protected override DB2Version Version => DB2Version.LUW;
	}

	/// <summary>DB2 for z/OS DML mechanics. Parameterless so the remote client can reconstruct it from its type name.</summary>
	public sealed class DB2zOSDmlService : DB2DmlService
	{
		protected override DB2Version Version => DB2Version.zOS;
	}
}
