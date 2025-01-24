using LinqToDB.CodeModel;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Provides access to built-in metadata generators.
	/// </summary>
	public static class MetadataBuilders
	{
		/// <summary>
		/// Gets metadata builder of specific type.
		/// </summary>
		/// <param name="languageProvider">Language provider.</param>
		/// <param name="metadataSource">Type of metadata source.</param>
		/// <returns>Attribute-based metadata builder instance or <c>null</c>.</returns>
		public static IMetadataBuilder? GetMetadataBuilder(ILanguageProvider languageProvider, MetadataSource metadataSource, IType? fluentBuilderType )
		{
			return metadataSource switch
			{
				MetadataSource.Attributes    => AttributeBasedMetadataBuilder.Instance,
				MetadataSource.FluentMapping => new FluentMetadataBuilder(languageProvider.ASTBuilder, fluentBuilderType ),
				_                            => null
			};
		}
	}
}
