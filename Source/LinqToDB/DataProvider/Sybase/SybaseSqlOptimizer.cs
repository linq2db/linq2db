namespace LinqToDB.DataProvider.Sybase
{
	using SqlProvider;
	using SqlQuery;

	class SybaseSqlOptimizer : BasicSqlOptimizer
	{
		public SybaseSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		protected static string[] SybaseCharactersToEscape = {"_", "%", "[", "]", "^"};

		public override string[] LikeCharactersToEscape => SybaseCharactersToEscape;

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
	}
}
