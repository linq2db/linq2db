using System;
using System.Linq.Expressions;

using Oracle.DataAccess.Types;
using LinqToDB.Expressions;

namespace LinqToDB.DataProvider
{
	using Common;
	using Mapping;

	public class OracleMappingSchema : MappingSchema
	{
		public OracleMappingSchema() : this(ProviderName.Oracle)
		{
		}

		protected OracleMappingSchema(string configuration) : base(configuration)
		{
			AddScalarType(typeof(OracleBFile),        OracleBFile.       Null, DataType.VarChar);    // ?
			AddScalarType(typeof(OracleBinary),       OracleBinary.      Null, DataType.VarBinary);
			AddScalarType(typeof(OracleBlob),         OracleBlob.        Null, DataType.VarBinary);  // ?
			AddScalarType(typeof(OracleClob),         OracleClob.        Null, DataType.NText);
			AddScalarType(typeof(OracleDate),         OracleDate.        Null, DataType.DateTime);
			AddScalarType(typeof(OracleDecimal),      OracleDecimal.     Null, DataType.Decimal);
			AddScalarType(typeof(OracleIntervalDS),   OracleIntervalDS.  Null, DataType.Time);       // ?
			AddScalarType(typeof(OracleIntervalYM),   OracleIntervalYM.  Null, DataType.Date);       // ?
			AddScalarType(typeof(OracleRef),          OracleRef.         Null, DataType.Binary);     // ?
			AddScalarType(typeof(OracleRefCursor),    OracleRefCursor.   Null, DataType.Binary);     // ?
			AddScalarType(typeof(OracleString),       OracleString.      Null, DataType.NVarChar);
			AddScalarType(typeof(OracleTimeStamp),    OracleTimeStamp.   Null, DataType.DateTime2);
			AddScalarType(typeof(OracleTimeStampLTZ), OracleTimeStampLTZ.Null, DataType.DateTimeOffset);
			AddScalarType(typeof(OracleTimeStampTZ),  OracleTimeStampTZ. Null, DataType.DateTimeOffset);
			AddScalarType(typeof(OracleXmlStream),    OracleXmlStream.   Null, DataType.Xml);        // ?
			AddScalarType(typeof(OracleXmlType),      OracleXmlType.     Null, DataType.Xml);
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
