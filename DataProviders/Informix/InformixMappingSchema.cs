using System;

using IBM.Data.Informix;

namespace LinqToDB.DataProvider
{
	using Mapping;

	public class InformixMappingSchema : MappingSchema
	{
		public InformixMappingSchema() : this(ProviderName.Informix)
		{
		}

		protected InformixMappingSchema(string configuration) : base(configuration)
		{
			AddScalarType(typeof(IfxBlob),        IfxBlob.       Null, DataType.VarBinary);
			AddScalarType(typeof(IfxClob),        IfxClob.       Null, DataType.Text);
			AddScalarType(typeof(IfxDateTime),    IfxDateTime.   Null, DataType.DateTime2);
			AddScalarType(typeof(IfxDecimal),     IfxDecimal.    Null, DataType.Decimal);
			AddScalarType(typeof(IfxTimeSpan),    IfxTimeSpan.   Null, DataType.Time);
			//AddScalarType(typeof(IfxMonthSpan),   IfxMonthSpan.  Null, DataType.Time);
		}
	}
}
