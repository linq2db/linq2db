using System;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	internal static class SqlOptimizerExtensions
	{
		public static SqlStatement OptimizeStatement(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, bool inlineParameters)
		{
			if (optimizer     == null) throw new ArgumentNullException(nameof(optimizer));
			if (statement     == null) throw new ArgumentNullException(nameof(statement));
			if (mappingSchema == null) throw new ArgumentNullException(nameof(mappingSchema));

			// transforming parameters to values
			var newStatement = statement.ProcessParameters(mappingSchema);

			// optimizing expressions according to new values
			newStatement = optimizer.OptimizeStatement(newStatement, inlineParameters);

			statement.UpdateIsParameterDepended();

			newStatement.SetAliases();

			// reset parameters
			newStatement.CollectParameters();

			return newStatement;
		}
	}
}
