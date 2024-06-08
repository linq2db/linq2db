using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider.Sybase
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	sealed class SybaseSqlOptimizer : BasicSqlOptimizer
	{
		public SybaseSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions)
		{
			return statement.QueryType switch
			{
				QueryType.Update => PrepareUpdateStatement((SqlUpdateStatement)statement),
				_ => statement,
			};
		}

		private static string[] SybaseCharactersToEscape = {"_", "%", "[", "]", "^"};

		public override string[] LikeCharactersToEscape => SybaseCharactersToEscape;

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);

			switch (func.Name)
			{
				case PseudoFunctions.REPLACE: return new SqlFunction(func.SystemType, "Str_Replace", func.IsAggregate, func.IsPure, func.Precedence, func.Parameters) { CanBeNull = func.CanBeNull };

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

				case PseudoFunctions.CONVERT:
				{
					var ftype = func.SystemType.ToUnderlying();
					if (ftype == typeof(string))
					{
						var stype = func.Parameters[2].SystemType!.ToUnderlying();

						if (stype == typeof(DateTime)
#if NET6_0_OR_GREATER
							|| stype == typeof(DateOnly)
#endif
							)
						{
							return new SqlFunction(func.SystemType, "convert", false, true, func.Parameters[0], func.Parameters[2], new SqlValue(23))
							{
								CanBeNull = func.CanBeNull
							};
						}
					}

					break;
				}
			}

			return base.ConvertFunction(func);
		}

		static SqlStatement PrepareUpdateStatement(SqlUpdateStatement statement)
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
						var sources = new HashSet<ISqlTableSource>() { tableToUpdate };
						if (sourceTable.Joins.Skip(i + 1).Any(j => QueryHelper.IsDependsOn(j, sources)))
							break;
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
