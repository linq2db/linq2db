#if NETFRAMEWORK
using System;

namespace LinqToDB.ServiceModel
{
	using SqlProvider;

	public class LinqServiceInfo
	{
		public string           MappingSchemaType     { get; set; } = null!;
		public string           SqlBuilderType        { get; set; } = null!;
		public string           SqlOptimizerType      { get; set; } = null!;
		public SqlProviderFlags SqlProviderFlags      { get; set; } = null!;
		public TableOptions     SupportedTableOptions { get; set; }
	}
}
#endif
