using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.DataProvider
{
	using Expressions;
	using Mapping;

	public abstract class DataProviderBase : IDataProvider
	{
		protected DataProviderBase(MappingSchema mappingSchema)
		{
			MappingSchema = mappingSchema;
		}

		public abstract string        Name          { get; }
		public abstract Type         ConnectionType { get; }
		public virtual  MappingSchema MappingSchema { get; private set; }

		public abstract IDbConnection CreateConnection (string connectionString);
		public abstract Expression    ConvertDataReader(Expression reader);

		public virtual Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			var mi = GetReaderMethodInfo(reader, idx, toType);

			if (mi == null)
				mi = MemberHelper.MethodOf<IDataRecord>(r => r.GetValue(0));

			var expr = Expression.Call(readerExpression, mi, Expression.Constant(idx)) as Expression;

			if (expr.Type == typeof(object))
			{
				var type = reader.GetFieldType(idx);

				if (type == typeof(byte[]))
					expr = Expression.Convert(expr, type);
			}

			return expr;
		}

		protected virtual MethodInfo GetReaderMethodInfo(IDataRecord reader, int idx, Type toType)
		{
			var type = reader.GetFieldType(idx);

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean  : return MemberHelper.MethodOf<IDataRecord>(r => r.GetBoolean (0));
				case TypeCode.Byte     : return MemberHelper.MethodOf<IDataRecord>(r => r.GetByte    (0));
				case TypeCode.Char     : return MemberHelper.MethodOf<IDataRecord>(r => r.GetChar    (0));
				case TypeCode.Int16    : return MemberHelper.MethodOf<IDataRecord>(r => r.GetInt16   (0));
				case TypeCode.Int32    : return MemberHelper.MethodOf<IDataRecord>(r => r.GetInt32   (0));
				case TypeCode.Int64    : return MemberHelper.MethodOf<IDataRecord>(r => r.GetInt64   (0));
				case TypeCode.Single   : return MemberHelper.MethodOf<IDataRecord>(r => r.GetFloat   (0));
				case TypeCode.Double   : return MemberHelper.MethodOf<IDataRecord>(r => r.GetDouble  (0));
				case TypeCode.String   : return MemberHelper.MethodOf<IDataRecord>(r => r.GetString  (0));
				case TypeCode.Decimal  : return MemberHelper.MethodOf<IDataRecord>(r => r.GetDecimal (0));
				case TypeCode.DateTime : return MemberHelper.MethodOf<IDataRecord>(r => r.GetDateTime(0));
			}

			if (type == typeof(Guid))   return MemberHelper.MethodOf<IDataRecord>(r => r.GetGuid(0));
			if (type == typeof(byte[])) return MemberHelper.MethodOf<IDataRecord>(r => r.GetValue(0));

			return null;
		}

		public virtual bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			return null;
		}

		public virtual void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			parameter.ParameterName = name;
			SetParameterType(parameter, dataType);
			parameter.Value = value ?? DBNull.Value;
		}

		public virtual void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			DbType dbType;

			switch (dataType)
			{
				case DataType.Char           : dbType = DbType.AnsiStringFixedLength; break;
				case DataType.VarChar        : dbType = DbType.AnsiString;            break;
				case DataType.NChar          : dbType = DbType.StringFixedLength;     break;
				case DataType.NVarChar       : dbType = DbType.String;                break;
				case DataType.VarBinary      : dbType = DbType.Binary;                break;
				case DataType.Boolean        : dbType = DbType.Boolean;               break;
				case DataType.SByte          : dbType = DbType.SByte;                 break;
				case DataType.Int16          : dbType = DbType.Int16;                 break;
				case DataType.Int32          : dbType = DbType.Int32;                 break;
				case DataType.Int64          : dbType = DbType.Int64;                 break;
				case DataType.Byte           : dbType = DbType.Byte;                  break;
				case DataType.UInt16         : dbType = DbType.UInt16;                break;
				case DataType.UInt32         : dbType = DbType.UInt32;                break;
				case DataType.UInt64         : dbType = DbType.UInt64;                break;
				case DataType.Single         : dbType = DbType.Single;                break;
				case DataType.Double         : dbType = DbType.Double;                break;
				case DataType.Decimal        : dbType = DbType.Decimal;               break;
				case DataType.Guid           : dbType = DbType.Guid;                  break;
				case DataType.Date           : dbType = DbType.Date;                  break;
				case DataType.Time           : dbType = DbType.Time;                  break;
				case DataType.DateTime       : dbType = DbType.DateTime;              break;
				case DataType.DateTime2      : dbType = DbType.DateTime2;             break;
				case DataType.DateTimeOffset : dbType = DbType.DateTimeOffset;        break;
				case DataType.Variant        : dbType = DbType.Object;                break;
				case DataType.VarNumeric     : dbType = DbType.VarNumeric;            break;
				default                      : return;
			}

			parameter.DbType = dbType;
		}
	}
}
