using System;
using System.Linq.Expressions;

using Oracle.DataAccess.Types;

namespace LinqToDB.DataProvider
{
	using Common;
	using Expressions;
	using Mapping;

	public class OracleMappingSchema : MappingSchema
	{
		public OracleMappingSchema() : this(ProviderName.Oracle)
		{
		}

		protected OracleMappingSchema(string configuration) : base(configuration)
		{
			AddScalarType(typeof(OracleBFile),        OracleBFile.       Null, true, DataType.VarChar);    // ?
			AddScalarType(typeof(OracleBinary),       OracleBinary.      Null, true, DataType.VarBinary);
			AddScalarType(typeof(OracleBlob),         OracleBlob.        Null, true, DataType.VarBinary);  // ?
			AddScalarType(typeof(OracleClob),         OracleClob.        Null, true, DataType.NText);
			AddScalarType(typeof(OracleDate),         OracleDate.        Null, true, DataType.DateTime);
			AddScalarType(typeof(OracleDecimal),      OracleDecimal.     Null, true, DataType.Decimal);
			AddScalarType(typeof(OracleIntervalDS),   OracleIntervalDS.  Null, true, DataType.Time);       // ?
			AddScalarType(typeof(OracleIntervalYM),   OracleIntervalYM.  Null, true, DataType.Date);       // ?
			AddScalarType(typeof(OracleRef),          OracleRef.         Null, true, DataType.Binary);     // ?
			AddScalarType(typeof(OracleRefCursor),    OracleRefCursor.   Null, true, DataType.Binary);     // ?
			AddScalarType(typeof(OracleString),       OracleString.      Null, true, DataType.NVarChar);
			AddScalarType(typeof(OracleTimeStamp),    OracleTimeStamp.   Null, true, DataType.DateTime2);
			AddScalarType(typeof(OracleTimeStampLTZ), OracleTimeStampLTZ.Null, true, DataType.DateTimeOffset);
			AddScalarType(typeof(OracleTimeStampTZ),  OracleTimeStampTZ. Null, true, DataType.DateTimeOffset);
			AddScalarType(typeof(OracleXmlStream),    OracleXmlStream.   Null, true, DataType.Xml);        // ?
			AddScalarType(typeof(OracleXmlType),      OracleXmlType.     Null, true, DataType.Xml);
		}

		public override LambdaExpression TryGetConvertExpression(Type from, Type to)
		{
			if (to.IsEnum && from == typeof(decimal))
			{
				var type = Converter.GetDefaultMappingFromEnumType(this, to);

				if (type != null)
				{
					var fromDecimalToType = GetConvertExpression(from, type, false);
					var fromTypeToEnum    = GetConvertExpression(type, to,   false);

					return Expression.Lambda(
						fromTypeToEnum.GetBody(fromDecimalToType.Body),
						fromDecimalToType.Parameters);
				}
			}

			return base.TryGetConvertExpression(from, to);
		}
	}
}
