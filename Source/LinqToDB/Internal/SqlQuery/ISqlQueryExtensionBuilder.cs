using System.Text;

using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.SqlQuery
{
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
		void Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension);
	}
}
