using System.Text;
using LinqToDB.Schema;
using LinqToDB.CodeModel;
using LinqToDB.SqlProvider;

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

		private static string BuildFQN(ISqlBuilder sqlBuilder, ObjectName name)
		{
			// TODO: linq2db fix required
			// currently we miss FQN-based mapping support at least for for stored procedures and non-table functions
			// and they require raw SQL name instead
			// we use BuildTableName as there is no separate API for function-like objects (but at least it doesn't make difference in generated SQL)
			return sqlBuilder.BuildTableName(
				new StringBuilder(),
				name.Server   == null ? null : sqlBuilder.ConvertInline(name.Server  , ConvertType.NameToServer    ),
				name.Database == null ? null : sqlBuilder.ConvertInline(name.Database, ConvertType.NameToDatabase  ),
				name.Schema   == null ? null : sqlBuilder.ConvertInline(name.Schema  , ConvertType.NameToSchema    ),
											   // NameToQueryTable used as we don't have separate ConvertType for procedures/functions
											   sqlBuilder.ConvertInline(name.Name    , ConvertType.NameToQueryTable),
				TableOptions.NotSet
			).ToString();
		}
	}
}
