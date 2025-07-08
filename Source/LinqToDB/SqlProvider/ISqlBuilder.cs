using System.Collections.Generic;
using System.Data.Common;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	public interface ISqlBuilder
	{
		int  CommandCount(SqlStatement statement);

		void BuildSql(int commandNumber, SqlStatement statement, StringBuilder sb, OptimizationContext optimizationContext, AliasesContext aliases, NullabilityContext? nullabilityContext,
			int           startIndent = 0);
		/// <summary>
		/// Writes database object name into provided <see cref="StringBuilder"/> instance.
		/// </summary>
		/// <param name="sb">String builder for generated object name.</param>
		/// <param name="name">Name of database object (e.g. table, view, procedure or function).</param>
		/// <param name="objectType">Type of database object, used to select proper name converter.</param>
		/// <param name="escape">If <c>true</c>, apply required escaping to name components. Must be <c>true</c> except rare cases when escaping is not needed.</param>
		/// <param name="tableOptions">Table options if called for table. Used to properly generate names for temporary tables.</param>
		/// <param name="withoutSuffix">If object name have suffix, which could be detached from main name, this parameter disables suffix generation (enables generation of only main name part).</param>
		/// <returns><paramref name="sb"/> parameter value.</returns>
		StringBuilder BuildObjectName               (StringBuilder sb, SqlObjectName name, ConvertType objectType = ConvertType.NameToQueryTable, bool escape = true, TableOptions tableOptions = TableOptions.NotSet, bool withoutSuffix = false);
		StringBuilder   BuildDataType                 (StringBuilder    sb,    DbDataType dataType);
		string          ConvertInline                 (string           value, ConvertType convertType);
		StringBuilder   Convert                       (StringBuilder    sb,    string      value, ConvertType convertType);
		ISqlExpression? GetIdentityExpression         (SqlTable         table);
		StringBuilder   PrintParameters               (IDataContext     dataContext, StringBuilder               sb, IEnumerable<DbParameter>? parameters);
		string          ApplyQueryHints               (string           sqlText,     IReadOnlyCollection<string> queryHints);
		string          GetReserveSequenceValuesSql   (int              count,       string                      sequenceName);
		string          GetMaxValueSql                (EntityDescriptor entity,      ColumnDescriptor            column);
		void BuildExpression(StringBuilder sb, ISqlExpression expr, bool buildTableName, object? context = null);

		string                                 Name             { get; }
		MappingSchema                          MappingSchema    { get; }
		StringBuilder                          StringBuilder    { get; }
		SqlProviderFlags                       SqlProviderFlags { get; }
		public Dictionary<string,TableIDInfo>? TableIDs         { get; }
		string?                                TablePath        { get; }
		string?                                QueryName        { get; }

		string? BuildSqlID(Sql.SqlID id);
	}
}
