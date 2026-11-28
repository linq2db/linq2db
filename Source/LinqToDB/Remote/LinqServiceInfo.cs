using System.Runtime.Serialization;

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
		/// Type of the identifier service the server-side provider uses, so client-side SQL preview
		/// aliases a statement the way the server does. The policy is transported by type: the client
		/// creates the type by name with its public parameterless constructor, which reproduces services
		/// whose rules the type alone defines, not instance state such as constructor arguments. When the
		/// client cannot load the type, the type is not an identifier service, or it has no public
		/// parameterless constructor, the client uses its default service; so it does when this member is
		/// <see langword="null"/> (a server that predates it). An exception thrown by the constructor
		/// itself propagates. Only the preview is affected: the server aliases the executed statement
		/// with its own service.
		/// </summary>
		[DataMember(Order = 9)]
		public string?          IdentifierServiceType    { get; set; }
	}
}
