namespace LinqToDB.DataProvider.Sybase
{
	using SqlProvider;
	using SqlQuery;
	using Mapping;

	class SybaseSqlOptimizer : BasicSqlOptimizer
	{
		public SybaseSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			return statement.QueryType switch
			{
				QueryType.Update => PrepareUpdateStatement((SqlUpdateStatement)statement),
				_ => statement,
			};
		}

		protected static string[] SybaseCharactersToEscape = {"_", "%", "[", "]", "^"};

		public override string[] LikeCharactersToEscape => SybaseCharactersToEscape;

		public override ISqlPredicate ConvertSearchStringPredicate<TContext>(MappingSchema mappingSchema, SqlPredicate.SearchString predicate, ConvertVisitor<RunOptimizationContext<TContext>> visitor,
			OptimizationContext optimizationContext)
		{
			if (predicate.IgnoreCase)
			{
				predicate = new SqlPredicate.SearchString(
					new SqlFunction(typeof(string), "$ToLower$", predicate.Expr1),
					predicate.IsNot,
					new SqlFunction(typeof(string), "$ToLower$", predicate.Expr2), 
					predicate.Kind,
					false);
			}

			return ConvertSearchStringPredicateViaLike(mappingSchema, predicate, visitor, optimizationContext);
		}

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);

			switch (func.Name)
			{
				case "$Replace$": return new SqlFunction(func.SystemType, "Str_Replace", func.IsAggregate, func.IsPure, func.Precedence, func.Parameters);

				case "CharIndex":
				{
					if (func.Parameters.Length == 3)
						return Add<int>(
							new SqlFunction(func.SystemType, "CharIndex",
								func.Parameters[0],
								new SqlFunction(typeof(string), "Substring",
									func.Parameters[1],
									func.Parameters[2],
									new SqlFunction(typeof(int), "Len", func.Parameters[1]))),
							Sub(func.Parameters[2], 1));
					break;
				}

				case "Stuff":
				{
					if (func.Parameters[3] is SqlValue value)
					{
						if (value.Value is string @string && string.IsNullOrEmpty(@string))
							return new SqlFunction(
								func.SystemType,
								func.Name,
								false,
								func.Precedence,
								func.Parameters[0],
								func.Parameters[1],
								func.Parameters[1],
								new SqlValue(value.ValueType, null));
					}

					break;
				}
			}

			return base.ConvertFunction(func);
		}

		SqlStatement PrepareUpdateStatement(SqlUpdateStatement statement)
		{
			var tableToUpdate = statement.Update.Table;

			if (tableToUpdate == null)
				return statement;

			if (statement.SelectQuery.From.Tables.Count > 0)
			{ 
				if (tableToUpdate == statement.SelectQuery.From.Tables[0].Source)
					return statement;

				var sourceTable = statement.SelectQuery.From.Tables[0];

				for (int i = 0; i < sourceTable.Joins.Count; i++)
				{
					var join = sourceTable.Joins[i];
					if (join.Table.Source == tableToUpdate)
					{
						statement.SelectQuery.From.Tables.Insert(0, join.Table);
						statement.SelectQuery.Where.SearchCondition.EnsureConjunction().Conditions
							.Add(new SqlCondition(false, join.Condition));

						sourceTable.Joins.RemoveAt(i);

						break;
					}
				}
			}

			return statement;
		}
	}
}
