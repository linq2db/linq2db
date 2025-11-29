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
		public string MemberConverterType                { get; set; } = null!;
	}
}
