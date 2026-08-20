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
		/// Identifier service the server-side provider uses, so client-side SQL preview aliases a
		/// statement the same way the server does when it renders it for real. Carried as a type name
		/// like the other provider services, which reproduces the whole policy rather than the few
		/// numbers a scalar contract could hold. <see langword="null"/> when the server predates this
		/// member; the client then falls back to its historical default.
		/// </summary>
		[DataMember(Order = 9)]
		public string?          IdentifierServiceType    { get; set; }
	}
}
