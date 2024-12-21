using System.Collections.Generic;

using LinqToDB.SchemaProvider;

namespace LinqToDB.DataProvider.SapHana
{
	public class ViewWithParametersTableSchema : TableSchema
	{
		public ViewWithParametersTableSchema()
		{
			IsProviderSpecific = true;
		}

		public List<ParameterSchema>? Parameters { get; set; }
	}
}
