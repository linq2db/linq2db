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
		public abstract string        ProviderName  { get; }
		public virtual  MappingSchema MappingSchema { get; private set; }

		public abstract IDbConnection CreateConnection (string connectionString);
		public abstract Expression    ConvertDataReader(Expression reader);

		public virtual void Configure(string name, string value)
		{
		}

		public virtual Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			var expr = GetReaderMethodExpression(reader, idx, readerExpression, toType);
			var conv = mappingSchema.GetConvertExpression(expr.Type, toType);

			if (conv.Body.GetCount(e => e == conv.Parameters[0]) > 1)
			{
				var variable = Expression.Variable(expr.Type, "value" + idx);
				var assign   = Expression.Assign(variable, expr);

				expr = Expression.Block(new[] { variable }, new[] { assign, conv.Body.Transform(e => e == conv.Parameters[0] ? variable : e) });
			}
			else
			{
				var ex = expr;
				expr = conv.Body.Transform(e => e == conv.Parameters[0] ? ex : e);
			}

			return expr;
		}

		protected virtual Expression GetReaderMethodExpression(IDataRecord reader, int idx, Expression readerExpression, Type toType)
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
	}
}
