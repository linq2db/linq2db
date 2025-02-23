using System;

using LinqToDB.Data;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	public class GetHanaSchemaOptions: GetSchemaOptions
	{
		public Func<ProcedureSchema, DataParameter[]?> GetStoredProcedureParameters = schema => null;
		public bool ThrowExceptionIfCalculationViewsNotAuthorized;
	}
}
