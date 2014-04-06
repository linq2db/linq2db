using System;

namespace LinqToDB.SchemaProvider
{
	using Data;

	public interface ISchemaProvider
	{
		DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions options = null);
	}
}
