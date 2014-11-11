using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.SapHana
{
    using SchemaProvider;


    public class ViewWithParametersTableSchema : TableSchema
    {
        public ViewWithParametersTableSchema()
        {
            IsProviderSpecific = true;
        }
        public List<ParameterSchema> Parameters { get; set; }
    }

}
