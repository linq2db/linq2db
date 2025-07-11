using System.Text;

using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.SqlQuery
{
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
		void Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension, SqlTable table, string alias);
	}
}
