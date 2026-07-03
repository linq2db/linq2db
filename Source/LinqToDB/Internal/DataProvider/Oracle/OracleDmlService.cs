using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	/// <summary>
	/// Oracle DML mechanics. An identity insert returns the generated key through an OUT parameter (the insert renders
	/// a trailing <c>RETURNING … INTO :IDENTITY_PARAMETER</c>). A create-table-with-identity emits the table plus a
	/// <c>CREATE SEQUENCE</c> and a <c>CREATE OR REPLACE TRIGGER</c>; a truncate-with-identity-reset emits the truncate
	/// plus a PL/SQL block that walks the identity sequence back to zero. The auxiliary commands are rendered as
	/// <see cref="SqlFragmentStatement"/>s (identity names carried as <see cref="SqlObjectNameExpression"/>s so the
	/// builder quotes them). Everything else uses the base scenario.
	/// </summary>
	public sealed class OracleDmlService : DmlServiceBase
	{
		// Oracle's identity sequence / trigger object names (mirrors OracleSqlBuilderBase.MakeIdentity*Name, which the
		// builder still uses for DROP).
		static string SequenceName(string tableName) => "SIDENTITY_" + tableName;
		static string TriggerName (string tableName) => "TIDENTITY_" + tableName;

		protected override bool IsTableNotFoundExceptionCore(Exception exception) => false;

		public override SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			switch (statement)
			{
				case SqlTruncateTableStatement { ResetIdentity: true } truncate when truncate.Table!.IdentityFields.Count > 0:
				{
					// No schema on the reset sequence reference (matches the previous BuildCommand behavior).
					var seq   = new SqlObjectNameExpression(new SqlObjectName(SequenceName(truncate.Table.TableName.Name)), ConvertType.SequenceName);
					var reset = new SqlFragmentStatement(factory.Fragment(
						  "DECLARE\n\tl_value number;\nBEGIN\n"
						+ "\tEXECUTE IMMEDIATE 'SELECT {0}.NEXTVAL FROM dual' INTO l_value;\n"
						+ "\tEXECUTE IMMEDIATE 'ALTER SEQUENCE {0} INCREMENT BY -' || l_value || ' MINVALUE 0';\n"
						+ "\tEXECUTE IMMEDIATE 'SELECT {0}.NEXTVAL FROM dual' INTO l_value;\n"
						+ "\tEXECUTE IMMEDIATE 'ALTER SEQUENCE {0} INCREMENT BY 1 MINVALUE 0';\n"
						+ "END;",
						seq));

					return new SqlCommandScenario
					{
						Steps =
						[
							new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery },
							new SqlCommandStep { Statement = reset,     Kind = SqlStepKind.NonQuery },
						],
						OutcomeSteps = [0],
					};
				}

				case SqlCreateTableStatement createTable when createTable.Table!.IdentityFields.Count > 0:
				{
					var table  = createTable.Table;
					var field  = table.IdentityFields[0];
					var schema = table.TableName.Schema;

					var sequence = new SqlFragmentStatement(factory.Fragment(
						"CREATE SEQUENCE {0}",
						new SqlObjectNameExpression(new SqlObjectName(SequenceName(table.TableName.Name), Schema: schema), ConvertType.SequenceName)));

					var trigger = new SqlFragmentStatement(factory.Fragment(
						  "CREATE OR REPLACE TRIGGER {0}\nBEFORE INSERT ON {1} FOR EACH ROW\nBEGIN\n"
						+ "\tSELECT {2}.NEXTVAL INTO :NEW.{3} FROM dual;\nEND;",
						new SqlObjectNameExpression(new SqlObjectName(TriggerName(table.TableName.Name), Schema: schema),  ConvertType.TriggerName),
						new SqlObjectNameExpression(table.TableName,                                                       ConvertType.NameToQueryTable),
						new SqlObjectNameExpression(new SqlObjectName(SequenceName(table.TableName.Name), Schema: schema), ConvertType.SequenceName),
						new SqlObjectNameExpression(new SqlObjectName(field.PhysicalName),                                 ConvertType.NameToQueryField)));

					return new SqlCommandScenario
					{
						Steps =
						[
							new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery },
							new SqlCommandStep { Statement = sequence,  Kind = SqlStepKind.NonQuery },
							new SqlCommandStep { Statement = trigger,   Kind = SqlStepKind.NonQuery },
						],
						OutcomeSteps = [0],
					};
				}

				case SqlInsertStatement { Insert.WithIdentity: true }:
				{
					// Oracle returns the generated identity through an OUT parameter — the insert renders a trailing
					// RETURNING <id> INTO :IDENTITY_PARAMETER; the interpreter adds the output parameter and reads it
					// back after ExecuteNonQuery. Replaces the legacy IsIdentityParameterRequired fast path for Oracle.
					return new SqlCommandScenario
					{
						Steps        = [new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery, OutParameterName = "IDENTITY_PARAMETER" }],
						OutcomeSteps = [0],
					};
				}
			}

			return base.BuildCommandScenario(statement, flags, factory);
		}
	}
}
