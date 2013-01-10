using System;

using NpgsqlTypes;

namespace LinqToDB.DataProvider
{
	using Mapping;

	class PostgreSQLMappingSchema : MappingSchema
	{
		public PostgreSQLMappingSchema() : base(ProviderName.PostgreSQL)
		{
//			AddScalarType(typeof(Fastpath));
//			AddScalarType(typeof(LargeObject));

			AddScalarType(typeof(BitString));
//			AddScalarType(typeof(NpgsqlBox));
//			AddScalarType(typeof(NpgsqlCircle));
			AddScalarType(typeof(NpgsqlDate));
//			AddScalarType(typeof(NpgsqlInet));
			AddScalarType(typeof(NpgsqlInterval));
//			AddScalarType(typeof(NpgsqlLSeg));
//			AddScalarType(typeof(NpgsqlMacAddress));
//			AddScalarType(typeof(NpgsqlPath));
			AddScalarType(typeof(NpgsqlPoint));
//			AddScalarType(typeof(NpgsqlPolygon));
//			AddScalarType(typeof(NpgsqlTime));
//			AddScalarType(typeof(NpgsqlTimeStamp));
//			AddScalarType(typeof(NpgsqlTimeStampTZ));
			AddScalarType(typeof(NpgsqlTimeTZ));
//			AddScalarType(typeof(NpgsqlTimeZone));
		}
	}
}
