using System.Text;

using LinqToDB.SqlProvider;

namespace LinqToDB.SqlQuery
{
	/// <summary>
	/// Base interface for all extension builders.
	/// </summary>
	public interface ISqlExtensionBuilder
	{
	}

	/// <summary>
	/// Interface for custom query extension builder.
	/// </summary>
	public interface ISqlQueryExtensionBuilder : ISqlExtensionBuilder
	{
		/// <summary>
		/// Emits query extension SQL.
		/// </summary>
		/// <param name="nullability">Current nullability context.</param>
		/// <param name="sqlBuilder">SQL builder interface.</param>
		/// <param name="stringBuilder">String builder to emit extension SQL to.</param>
		/// <param name="sqlQueryExtension">Extension instance.</param>
		void Build(NullabilityContext nullability, SqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension);
	}

	/// <summary>
	/// Interface for custom table extension builder.
	/// </summary>
	public interface ISqlTableExtensionBuilder : ISqlExtensionBuilder
	{
		/// <summary>
		/// Emits table extension SQL.
		/// </summary>
		/// <param name="nullability">Current nullability context.</param>
		/// <param name="sqlBuilder">SQL builder interface.</param>
		/// <param name="stringBuilder">String builder to emit extension SQL to.</param>
		/// <param name="sqlQueryExtension">Extension instance.</param>
		/// <param name="table">Target table.</param>
		/// <param name="alias">Target table alias.</param>
		void Build(NullabilityContext nullability, SqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension, SqlTable table, string alias);
	}
}
