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
		internal static Func<ISqlExpression,ISqlExpression,bool> DefaultComparer = (x, y) => true;

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static bool IsInsert(this SqlStatement statement)
		{
			return
				statement.QueryType == QueryType.Insert ||
				statement.QueryType == QueryType.InsertOrUpdate ||
				statement.QueryType == QueryType.MultiInsert;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static bool NeedsIdentity(this SqlStatement statement)
		{
			return statement.QueryType == QueryType.Insert && ((SqlInsertStatement)statement).Insert.WithIdentity;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static bool IsUpdate(this SqlStatement statement)
		{
			return statement != null && statement.QueryType == QueryType.Update;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static bool IsDelete(this SqlStatement statement)
		{
			return statement != null && statement.QueryType == QueryType.Delete;
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
		public static SqlInsertClause? GetInsertClause(this SqlStatement statement)
		{
			return statement switch
			{
				SqlInsertStatement insert         => insert.Insert,
				SqlInsertOrUpdateStatement update => update.Insert,
				_                                 => null,
			};
		}

		public static SqlWithClause? GetWithClause(this SqlStatement statement)
		{
			return statement switch
			{
				SqlStatementWithQueryBase query => query.With,
				_                               => null,
			};
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static SqlOutputClause? GetOutputClause(this SqlStatement statement)
		{
			return statement switch
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
		public static SelectQuery EnsureQuery(this SqlStatement statement)
		{
			var selectQuery = statement.SelectQuery;
			if (selectQuery == null)
				throw new LinqToDBException("Sqlect Query required");
			return selectQuery;
		}

		internal static bool IsSqlRow(this Expression expression)
			=> expression.Type.IsSqlRow();

		private static bool IsSqlRow(this Type type)
			=> type.IsGenericType == true && type.GetGenericTypeDefinition() == typeof(Sql.SqlRow<,>);

		internal static ReadOnlyCollection<Expression> GetSqlRowValues(this Expression expr)
		{
			return expr is MethodCallExpression { Method.Name: "Row" } call
				? call.Arguments
				: throw new LinqToDBException("Calls to Sql.Row() are the only valid expressions of type SqlRow.");
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
			if (name == func.Name)
				return func;

			return new SqlFunction(func.Type, name, func.Flags, func.NullabilityType, func.CanBeNullNullable, func.Parameters)
			{
				DoNotOptimize = func.DoNotOptimize
			};
		}
	}
}
