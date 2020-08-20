using System;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	internal static class SqlOptimizerExtensions
	{
		public static SqlStatement OptimizeStatement(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, bool inlineParameters, bool remoteContext)
		{
			if (optimizer     == null) throw new ArgumentNullException(nameof(optimizer));
			if (statement     == null) throw new ArgumentNullException(nameof(statement));
			if (mappingSchema == null) throw new ArgumentNullException(nameof(mappingSchema));

			BuildSqlValueTableParameters(statement);

			// transforming parameters to values
			var newStatement = statement.ProcessParameters(mappingSchema);

			newStatement.UpdateIsParameterDepended();

			// optimizing expressions according to new values
			newStatement = optimizer.OptimizeStatement(newStatement, inlineParameters,
				newStatement.IsParameterDependent || inlineParameters, remoteContext);

			newStatement.PrepareQueryAndAliases();

			return newStatement;
		}

		static void BuildSqlValueTableParameters(SqlStatement statement)
		{
			if (statement.IsParameterDependent)
			{
				new QueryVisitor().Visit(statement, e =>
				{
					if (e is SqlValuesTable table)
						table.BuildRows();
				});
			}
		}


	}
}
