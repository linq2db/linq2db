namespace LinqToDB.DataProvider.SapHana
{
	using System.Collections.Generic;
	using LinqToDB.Common;
	using LinqToDB.SqlQuery;
	using SqlProvider;

	class SapHanaNativeSqlOptimizer : SapHanaSqlOptimizer
	{
		public SapHanaNativeSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement OptimizeStatement(SqlStatement statement, bool inlineParameters, bool withParameters, bool remoteContext)
		{
			statement = base.OptimizeStatement(statement, inlineParameters, withParameters, remoteContext);

			if (remoteContext)
				return statement;

			// SAP HANA parameters are not just order-dependent but also name-dependent, so we cannot use
			// same parameter name
			var parameters = new HashSet<SqlParameter>(Utils.ObjectReferenceEqualityComparer<SqlParameter>.Default);

			statement.Parameters.Clear();

			// duplicate parameters
			statement = ConvertVisitor.ConvertAll(statement, (visitor, e) =>
			{
				if (e is ISqlExpression expr)
				{
					if (HasParameters(expr))
					{
						// prevent skipping of duplicate elements
						visitor.VisitedElements[e] = null;
					}

					if (e is SqlParameter p && !parameters.Add(p))
					{
						p = p.Clone();
						parameters.Add(p);
						statement.IsParameterDependent = true;
						return p;
					}
				}

				return e;
			});

			statement.Parameters.AddRange(parameters);

			return statement;
		}
	}

}
