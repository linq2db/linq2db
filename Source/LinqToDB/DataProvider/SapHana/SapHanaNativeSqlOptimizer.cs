namespace LinqToDB.DataProvider.SapHana
{
	using System;
	using System.Collections.Generic;
	using LinqToDB.Common;
	using LinqToDB.SqlQuery;
	using SqlProvider;

	class SapHanaNativeSqlOptimizer : SapHanaSqlOptimizer
	{
		public SapHanaNativeSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement OptimizeStatement(SqlStatement statement, bool inlineParameters, bool withParameters)
		{
			statement = base.OptimizeStatement(statement, inlineParameters, withParameters);

			// SAP HANA parameters are not just order-dependent but also name-dependent, so we cannot use
			// same parameter name
			var parameters = new HashSet<SqlParameter>(Utils.ObjectReferenceEqualityComparer<SqlParameter>.Default);

			if (statement.Parameters.Count > 0)
				return statement;

			// duplicate parameters
			statement = ConvertVisitor.ConvertAll(statement, (visitor, e) =>
			{
				// prevent skipping of duplicate elements
				visitor.VisitedElements[e] = null;

				if (e is SqlParameter p)
				{
					if (!parameters.Add(p))
					{
						p = p.Clone();
						parameters.Add(p);
						visitor.VisitedElements[p] = null;
						statement.IsParameterDependent = true;
						return p;
					}
				}

				return e;
			});

			// assign unique names to parameters
			var allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			Utils.MakeUniqueNames(
				parameters,
				allNames,
				(n, a) => !a!.Contains(n) && !ReservedWords.IsReserved(n), p => p.Name, (p, n, a) =>
				{
					a!.Add(n);
					p.Name = n;
				},
				p => p.Name.IsNullOrEmpty() ? "p1" : p.Name + "_1",
				StringComparer.OrdinalIgnoreCase);

			return statement;
		}
	}

}
