using System;

namespace LinqToDB.DataProvider.SchemaProvider
{
	using Data;

	public interface ISchemaProvider
	{
		DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions options = null);
	}
}
