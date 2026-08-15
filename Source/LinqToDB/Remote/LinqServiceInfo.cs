using System.Runtime.Serialization;

using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Remote
{
	[DataContract]
	public class LinqServiceInfo
	{
		[DataMember(Order = 1)]
		public string           MappingSchemaType        { get; set; } = null!;
		[DataMember(Order = 2)]
		public string           SqlBuilderType           { get; set; } = null!;
		[DataMember(Order = 3)]
		public string           SqlOptimizerType         { get; set; } = null!;
		[DataMember(Order = 4)]
		public SqlProviderFlags SqlProviderFlags         { get; set; } = null!;
		[DataMember(Order = 5)]
		public TableOptions     SupportedTableOptions    { get; set; }
		[DataMember(Order = 6)]
		public string           MethodCallTranslatorType { get; set; } = null!;
		[DataMember(Order = 7)]
		public string           MemberConverterType      { get; set; } = null!;
		[DataMember(Order = 8)]
		public string?          DmlServiceType           { get; set; }

		/// <summary>
		/// Identifier length limit the server-side provider declares, so client-side SQL preview
		/// aliases the statement the same way the server does. Zero when the server predates this
		/// member; the client then falls back to its historical default.
		/// </summary>
		[DataMember(Order = 9)]
		public int                  IdentifierMaxLength  { get; set; }

		/// <summary>
		/// Unit <see cref="IdentifierMaxLength"/> is counted in.
		/// </summary>
		[DataMember(Order = 10)]
		public IdentifierLengthUnit IdentifierLengthUnit { get; set; }

		/// <summary>
		/// Alias length limit, which several providers set higher than <see cref="IdentifierMaxLength"/>
		/// - MySQL caps a name at 64 and an alias at 255. Zero when the server predates this member, in
		/// which case the client applies <see cref="IdentifierMaxLength"/> to aliases too.
		/// </summary>
		[DataMember(Order = 11)]
		public int                  IdentifierAliasMaxLength { get; set; }
	}
}
