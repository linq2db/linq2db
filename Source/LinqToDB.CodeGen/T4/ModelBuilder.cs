using LinqToDB.CodeGen.Metadata;
using LinqToDB.Data;

namespace LinqToDB.CodeGen.T4
{
	public static class ModelBuilder
	{
		public static DataModel LoadServerMetadata(DataConnection dataConnection, SchemaSettings settings)
		{
			return MetadataBuilder.LoadDataModel(dataConnection, settings);
		}
	}
}
