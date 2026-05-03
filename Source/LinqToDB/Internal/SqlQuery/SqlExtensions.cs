using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public static class SqlExtensions
	{
		internal static Func<ISqlExpression, ISqlExpression, bool> DefaultComparer = (x, y) => true;

		extension(SqlStatement statement)
		{
			/// <summary>
			/// This is internal API and is not intended for use by Linq To DB applications.
			/// It may change or be removed without further notice.
			/// </summary>
			public bool IsInsert =>
				statement.QueryType 
					is QueryType.Insert
					or QueryType.InsertOrUpdate
					or QueryType.MultiInsert;

			/// <summary>
			/// This is internal API and is not intended for use by Linq To DB applications.
			/// It may change or be removed without further notice.
			/// </summary>
			public bool NeedsIdentity => 
				statement is SqlInsertStatement { QueryType: QueryType.Insert, Insert.WithIdentity: true };

			/// <summary>
			/// This is internal API and is not intended for use by Linq To DB applications.
			/// It may change or be removed without further notice.
			/// </summary>
			public bool IsUpdate => statement is { QueryType: QueryType.Update };

			/// <summary>
			/// This is internal API and is not intended for use by Linq To DB applications.
			/// It may change or be removed without further notice.
			/// </summary>
			public bool IsDelete => statement is { QueryType: QueryType.Delete };

			/// <summary>
			/// This is internal API and is not intended for use by Linq To DB applications.
			/// It may change or be removed without further notice.
			/// </summary>
			public SqlInsertClause? InsertClause =>
				statement switch
				{
					SqlInsertStatement insert => insert.Insert,
					SqlInsertOrUpdateStatement update => update.Insert,
					_ => null,
				};

			public SqlWithClause? WithClause =>
				statement switch
				{
					SqlStatementWithQueryBase query => query.With,
					_ => null,
				};

			/// <summary>
			/// This is internal API and is not intended for use by Linq To DB applications.
			/// It may change or be removed without further notice.
			/// </summary>
			public SqlOutputClause? OutputClause =>
				statement switch
				{
					SqlInsertStatement insert => insert.Output,
					SqlUpdateStatement update => update.Output,
					SqlDeleteStatement delete => delete.Output,
					_ => null,
				};
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static bool HasSomeModifiers(this SqlSelectClause select, bool ignoreSkip, bool ignoreTake)
		{
			return select.IsDistinct || (!ignoreSkip && select.SkipValue != null) || (!ignoreTake && select.TakeValue != null);
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static SelectQuery EnsureQuery(this SqlStatement statement)
		{
			var selectQuery = statement.SelectQuery;
			if (selectQuery == null)
				throw new LinqToDBException("Sqlect Query required");
			return selectQuery;
		}

		extension(Expression expression)
		{
			internal bool IsSqlRow => 
				expression.Type.IsGenericType
				&& expression.Type.GetGenericTypeDefinition() == typeof(Sql.SqlRow<,>);
		}

		internal static SqlTable Set(this SqlTable table, bool? set, TableOptions tableOptions)
		{
			if (set.HasValue)
			{
				if (set.Value) table.TableOptions |=  tableOptions;
				else           table.TableOptions &= ~tableOptions;
			}

			return table;
		}

		public static SqlExtendedFunction CreateCount(this ISqlTableSource table, MappingSchema mappingSchema)
		{
			return new SqlExtendedFunction(mappingSchema.GetDbDataType(typeof(int)), "COUNT",
				// unused parameter to make expr unique
				[new SqlFunctionArgument(new SqlFragment("*", new SqlValue(table.SourceID)))],
				[],
				canBeNull: false,
				isAggregate: true,
				canBeAffectedByOrderBy: false);
		}

		public static SqlFunction WithName(this SqlFunction func, string name)
		{
			if (string.Equals(name, func.Name, StringComparison.Ordinal))
				return func;

			return new SqlFunction(func.Type, name, func.Flags, func.NullabilityType, func.CanBeNullNullable, func.Parameters)
			{
				DoNotOptimize = func.DoNotOptimize,
			};
		}
	}
}
