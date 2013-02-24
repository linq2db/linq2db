using System;
using System.Data.Common;

namespace LinqToDB.DataProvider.SchemaProvider
{
	using Data;

	public abstract class SchemaProviderBase : ISchemaProvider
	{
		public virtual DatabaseSchema GetSchema(DataConnection dataConnection)
		{
			var dbConnection = (DbConnection)dataConnection.Connection;

			var schema     = dbConnection.GetSchema();
			var tables     = dbConnection.GetSchema("Tables");
			var columns    = dbConnection.GetSchema("Columns");
			var allColumns = dbConnection.GetSchema("AllColumns");
			var views      = dbConnection.GetSchema("Views");
			var procedures = dbConnection.GetSchema("Procedures");



			return new DatabaseSchema();
		}
	}
}
