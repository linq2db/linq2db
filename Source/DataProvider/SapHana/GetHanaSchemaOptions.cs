using System;

namespace LinqToDB.DataProvider.SapHana
{

    using Data;
    using SchemaProvider;

    public class GetHanaSchemaOptions: GetSchemaOptions
    {
        public Func<ProcedureSchema, DataParameter[]> GetStoredProcedureParameters = schema => null;
    }
}
