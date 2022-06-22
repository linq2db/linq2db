using System.Text;
using LinqToDB.Schema;
using LinqToDB.CodeModel;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Provides access to built-in metadata generators.
	/// </summary>
	public static class MetadataBuilders
	{
		/// <summary>
		/// Gets default metadata builder, based on <see cref="Mapping"/> namespace attributes.
		/// </summary>
		/// <param name="languageProvider">Language provider.</param>
		/// <param name="sqlBuilder">Database-specific <see cref="ISqlBuilder"/> instance.</param>
		/// <returns>Attribute-based metadata builder instance.</returns>
		public static IMetadataBuilder GetAttributeBasedMetadataBuilder(ILanguageProvider languageProvider, ISqlBuilder sqlBuilder)
		{
			return new AttributeBasedMetadataBuilder(languageProvider.ASTBuilder, name => BuildFQN(sqlBuilder, name));
		}

		private static string BuildFQN(ISqlBuilder sqlBuilder, SqlObjectName name)
		{
			// TODO: linq2db fix required
			// currently we miss FQN-based mapping support at least for for stored procedures and non-table functions
			// and they require raw SQL name instead
			return sqlBuilder.BuildObjectName(new (), name, ConvertType.NameToProcedure).ToString();
		}
	}
}
