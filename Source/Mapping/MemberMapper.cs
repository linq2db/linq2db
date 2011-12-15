using System;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Xml;

using LinqToDB.Data.Sql;
using LinqToDB.Extensions;
using LinqToDB.Reflection;

using Convert = LinqToDB.Common.Convert;

namespace LinqToDB.Mapping
{
	public partial class MemberMapper
	{
		#region Init

		public virtual void Init(MapMemberInfo mapMemberInfo)
		{
			if (mapMemberInfo == null) throw new ArgumentNullException("mapMemberInfo");

			MapMemberInfo          = mapMemberInfo;
			Name                   = mapMemberInfo.Name;
			MemberName             = mapMemberInfo.MemberName;
			Storage                = mapMemberInfo.Storage;
			DbType                 = mapMemberInfo.DbType;
			_type                  = mapMemberInfo.Type;
			MemberAccessor         = mapMemberInfo.MemberAccessor;
			_complexMemberAccessor = mapMemberInfo.ComplexMemberAccessor;
			MappingSchema          = mapMemberInfo.MappingSchema;

			if (Storage != null)
				MemberAccessor = ExprMemberAccessor.GetMemberAccessor(MemberAccessor.TypeAccessor, Storage);
		}

		internal static MemberMapper CreateMemberMapper(MapMemberInfo mi)
		{
			var type = mi.Type;
			var mm   = null as MemberMapper;

			if (type.IsPrimitive || type.IsEnum)
				mm = GetPrimitiveMemberMapper(mi);

			if (mm == null) mm = GetNullableMemberMapper(mi);
			if (mm == null) mm = GetSimpleMemberMapper  (mi);
#if !SILVERLIGHT
			if (mm == null) mm = GetSqlTypeMemberMapper (mi);
#endif
			return mm ?? new DefaultMemberMapper();
		}

		#endregion

		#region Public Properties

		public MappingSchema  MappingSchema  { get; private set; }
		public string         Name           { get; private set; }
		public string         MemberName     { get; private set; }
		public string         Storage        { get; private set; }
		public DbType         DbType         { get; private set; }
		public MapMemberInfo  MapMemberInfo  { get; private set; }
		public int            Ordinal        { get; private set; }
		public MemberAccessor MemberAccessor { get; private set; }
		public bool           IsExplicit     { get; set;         }

		internal void SetOrdinal(int ordinal)
		{
			Ordinal = ordinal;
		}

		private MemberAccessor _complexMemberAccessor;
		public  MemberAccessor  ComplexMemberAccessor
		{
			[DebuggerStepThrough]
			get { return _complexMemberAccessor ?? MemberAccessor; }
		}

		Type _type;
		public virtual Type Type
		{
			get { return _type; }
		}

		public DbType GetDbType()
		{
			if (MapMemberInfo.IsDbTypeSet)
				return DbType;

			if (DbType != DbType.Object)
				return DbType;

			var dataType = SqlDataType.GetDataType(_type);

			switch (dataType.SqlDbType)
			{
				case SqlDbType.BigInt           : return DbType.Int64;
				case SqlDbType.Binary           : return DbType.Binary;
				case SqlDbType.Bit              : return DbType.Boolean;
				case SqlDbType.Char             : return DbType.AnsiStringFixedLength;
				case SqlDbType.DateTime         : return DbType.DateTime;
				case SqlDbType.Decimal          : return DbType.Decimal;
				case SqlDbType.Float            : return DbType.Double;
				case SqlDbType.Image            : return DbType.Binary;
				case SqlDbType.Int              : return DbType.Int32;
				case SqlDbType.Money            : return DbType.Currency;
				case SqlDbType.NChar            : return DbType.StringFixedLength;
				case SqlDbType.NText            : return DbType.String;
				case SqlDbType.NVarChar         : return DbType.String;
				case SqlDbType.Real             : return DbType.Single;
				case SqlDbType.UniqueIdentifier : return DbType.Guid;
				case SqlDbType.SmallDateTime    : return DbType.DateTime;
				case SqlDbType.SmallInt         : return DbType.Int16;
				case SqlDbType.SmallMoney       : return DbType.Currency;
				case SqlDbType.Text             : return DbType.AnsiString;
				case SqlDbType.Timestamp        : return DbType.Binary;
				case SqlDbType.TinyInt          : return DbType.Byte;
				case SqlDbType.VarBinary        : return DbType.Binary;
				case SqlDbType.VarChar          : return DbType.AnsiString;
				case SqlDbType.Variant          : return DbType.Object;
				case SqlDbType.Xml              : return DbType.Xml;
				case SqlDbType.Udt              : return DbType.Binary;
				case SqlDbType.Date             : return DbType.Date;
				case SqlDbType.Time             : return DbType.Time;
#if !MONO
				case SqlDbType.Structured       : return DbType.Binary;
				case SqlDbType.DateTime2        : return DbType.DateTime2;
				case SqlDbType.DateTimeOffset   : return DbType.DateTimeOffset;
#endif
			}

			return DbType.Object;
		}

		public int GetDbSize(object value)
		{
			if (MapMemberInfo.IsDbSizeSet)
				return MapMemberInfo.DbSize;

			if (value == null)
				return 0;

			if (value is string)
				return ((string)value).Length;

			if (value is byte[])
				return ((byte[])value).Length;


			var dataType = SqlDataType.GetDataType(_type);

			switch (dataType.SqlDbType)
			{
				case SqlDbType.BigInt           : return 0;
				case SqlDbType.Binary           : return 0;
				case SqlDbType.Bit              : return 0;
				case SqlDbType.Char             : return 0;
				case SqlDbType.DateTime         : return 0;
				case SqlDbType.Decimal          : return 0;
				case SqlDbType.Float            : return 0;
				case SqlDbType.Image            : return 0;
				case SqlDbType.Int              : return 0;
				case SqlDbType.Money            : return 0;
				case SqlDbType.NChar            : return 0;
				case SqlDbType.NText            : return 0;
				case SqlDbType.NVarChar         : return 0;
				case SqlDbType.Real             : return 0;
				case SqlDbType.UniqueIdentifier : return 0;
				case SqlDbType.SmallDateTime    : return 0;
				case SqlDbType.SmallInt         : return 0;
				case SqlDbType.SmallMoney       : return 0;
				case SqlDbType.Text             : return 0;
				case SqlDbType.Timestamp        : return 0;
				case SqlDbType.TinyInt          : return 0;
				case SqlDbType.VarBinary        : return 0;
				case SqlDbType.VarChar          : return 0;
				case SqlDbType.Variant          : return 0;
				case SqlDbType.Xml              : return 0;
				case SqlDbType.Udt              : return 0;
				case SqlDbType.Date             : return 0;
				case SqlDbType.Time             : return 0;
#if !MONO
				case SqlDbType.Structured       : return 0;
				case SqlDbType.DateTime2        : return 0;
				case SqlDbType.DateTimeOffset   : return 0;
#endif
			}

			return 0;
		}

		#endregion

		#region Default Members (GetValue, SetValue)

		public virtual bool SupportsValue { get { return !IsExplicit; } }

		public virtual object GetValue(object o)
		{
			return MemberAccessor.GetValue(o);
		}

		public virtual bool     IsNull     (object o) { return GetValue(o) == null; }

		// Simple type getters.
		//
		[CLSCompliant(false)]
		public virtual SByte    GetSByte   (object o) { return MemberAccessor.GetSByte   (o); }
		public virtual Int16    GetInt16   (object o) { return MemberAccessor.GetInt16   (o); }
		public virtual Int32    GetInt32   (object o) { return MemberAccessor.GetInt32   (o); }
		public virtual Int64    GetInt64   (object o) { return MemberAccessor.GetInt64   (o); }

		public virtual Byte     GetByte    (object o) { return MemberAccessor.GetByte    (o); }
		[CLSCompliant(false)]
		public virtual UInt16   GetUInt16  (object o) { return MemberAccessor.GetUInt16  (o); }
		[CLSCompliant(false)]
		public virtual UInt32   GetUInt32  (object o) { return MemberAccessor.GetUInt32  (o); }
		[CLSCompliant(false)]
		public virtual UInt64   GetUInt64  (object o) { return MemberAccessor.GetUInt64  (o); }

		public virtual Boolean  GetBoolean (object o) { return MemberAccessor.GetBoolean (o); }
		public virtual Char     GetChar    (object o) { return MemberAccessor.GetChar    (o); }
		public virtual Single   GetSingle  (object o) { return MemberAccessor.GetSingle  (o); }
		public virtual Double   GetDouble  (object o) { return MemberAccessor.GetDouble  (o); }
		public virtual Decimal  GetDecimal (object o) { return MemberAccessor.GetDecimal (o); }
		public virtual Guid     GetGuid    (object o) { return MemberAccessor.GetGuid    (o); }
		public virtual DateTime GetDateTime(object o) { return MemberAccessor.GetDateTime(o); }
		public virtual DateTimeOffset GetDateTimeOffset(object o) { return MemberAccessor.GetDateTimeOffset(o); }

		// Nullable type getters.
		//
		[CLSCompliant(false)]
		public virtual SByte?    GetNullableSByte   (object o) { return MemberAccessor.GetNullableSByte   (o); }
		public virtual Int16?    GetNullableInt16   (object o) { return MemberAccessor.GetNullableInt16   (o); }
		public virtual Int32?    GetNullableInt32   (object o) { return MemberAccessor.GetNullableInt32   (o); }
		public virtual Int64?    GetNullableInt64   (object o) { return MemberAccessor.GetNullableInt64   (o); }

		public virtual Byte?     GetNullableByte    (object o) { return MemberAccessor.GetNullableByte    (o); }
		[CLSCompliant(false)]
		public virtual UInt16?   GetNullableUInt16  (object o) { return MemberAccessor.GetNullableUInt16  (o); }
		[CLSCompliant(false)]
		public virtual UInt32?   GetNullableUInt32  (object o) { return MemberAccessor.GetNullableUInt32  (o); }
		[CLSCompliant(false)]
		public virtual UInt64?   GetNullableUInt64  (object o) { return MemberAccessor.GetNullableUInt64  (o); }

		public virtual Boolean?  GetNullableBoolean (object o) { return MemberAccessor.GetNullableBoolean (o); }
		public virtual Char?     GetNullableChar    (object o) { return MemberAccessor.GetNullableChar    (o); }
		public virtual Single?   GetNullableSingle  (object o) { return MemberAccessor.GetNullableSingle  (o); }
		public virtual Double?   GetNullableDouble  (object o) { return MemberAccessor.GetNullableDouble  (o); }
		public virtual Decimal?  GetNullableDecimal (object o) { return MemberAccessor.GetNullableDecimal (o); }
		public virtual Guid?     GetNullableGuid    (object o) { return MemberAccessor.GetNullableGuid    (o); }
		public virtual DateTime? GetNullableDateTime(object o) { return MemberAccessor.GetNullableDateTime(o); }
		public virtual DateTimeOffset? GetNullableDateTimeOffset(object o) { return MemberAccessor.GetNullableDateTimeOffset(o); }

#if !SILVERLIGHT

		// SQL type getters.
		//
		public virtual SqlByte     GetSqlByte    (object o) { return MemberAccessor.GetSqlByte    (o); }
		public virtual SqlInt16    GetSqlInt16   (object o) { return MemberAccessor.GetSqlInt16   (o); }
		public virtual SqlInt32    GetSqlInt32   (object o) { return MemberAccessor.GetSqlInt32   (o); }
		public virtual SqlInt64    GetSqlInt64   (object o) { return MemberAccessor.GetSqlInt64   (o); }
		public virtual SqlSingle   GetSqlSingle  (object o) { return MemberAccessor.GetSqlSingle  (o); }
		public virtual SqlBoolean  GetSqlBoolean (object o) { return MemberAccessor.GetSqlBoolean (o); }
		public virtual SqlDouble   GetSqlDouble  (object o) { return MemberAccessor.GetSqlDouble  (o); }
		public virtual SqlDateTime GetSqlDateTime(object o) { return MemberAccessor.GetSqlDateTime(o); }
		public virtual SqlDecimal  GetSqlDecimal (object o) { return MemberAccessor.GetSqlDecimal (o); }
		public virtual SqlMoney    GetSqlMoney   (object o) { return MemberAccessor.GetSqlMoney   (o); }
		public virtual SqlGuid     GetSqlGuid    (object o) { return MemberAccessor.GetSqlGuid    (o); }
		public virtual SqlString   GetSqlString  (object o) { return MemberAccessor.GetSqlString  (o); }

#endif

		public virtual void SetValue(object o, object value)
		{
			MemberAccessor.SetValue(o, value);
		}

		public virtual void SetNull   (object o)                { SetValue(o, null); }

		// Simple type setters.
		//
		[CLSCompliant(false)]
		public virtual void SetSByte   (object o, SByte    value) { MemberAccessor.SetSByte   (o, value); }
		public virtual void SetInt16   (object o, Int16    value) { MemberAccessor.SetInt16   (o, value); }
		public virtual void SetInt32   (object o, Int32    value) { MemberAccessor.SetInt32   (o, value); }
		public virtual void SetInt64   (object o, Int64    value) { MemberAccessor.SetInt64   (o, value); }

		public virtual void SetByte    (object o, Byte     value) { MemberAccessor.SetByte    (o, value); }
		[CLSCompliant(false)]
		public virtual void SetUInt16  (object o, UInt16   value) { MemberAccessor.SetUInt16  (o, value); }
		[CLSCompliant(false)]
		public virtual void SetUInt32  (object o, UInt32   value) { MemberAccessor.SetUInt32  (o, value); }
		[CLSCompliant(false)]
		public virtual void SetUInt64  (object o, UInt64   value) { MemberAccessor.SetUInt64  (o, value); }

		public virtual void SetBoolean (object o, Boolean  value) { MemberAccessor.SetBoolean (o, value); }
		public virtual void SetChar    (object o, Char     value) { MemberAccessor.SetChar    (o, value); }
		public virtual void SetSingle  (object o, Single   value) { MemberAccessor.SetSingle  (o, value); }
		public virtual void SetDouble  (object o, Double   value) { MemberAccessor.SetDouble  (o, value); }
		public virtual void SetDecimal (object o, Decimal  value) { MemberAccessor.SetDecimal (o, value); }
		public virtual void SetGuid    (object o, Guid     value) { MemberAccessor.SetGuid    (o, value); }
		public virtual void SetDateTime(object o, DateTime value) { MemberAccessor.SetDateTime(o, value); }
		public virtual void SetDateTimeOffset(object o, DateTimeOffset value) { MemberAccessor.SetDateTimeOffset(o, value); }

		// Nullable type setters.
		//
		[CLSCompliant(false)]
		public virtual void SetNullableSByte   (object o, SByte?    value) { MemberAccessor.SetNullableSByte   (o, value); }
		public virtual void SetNullableInt16   (object o, Int16?    value) { MemberAccessor.SetNullableInt16   (o, value); }
		public virtual void SetNullableInt32   (object o, Int32?    value) { MemberAccessor.SetNullableInt32   (o, value); }
		public virtual void SetNullableInt64   (object o, Int64?    value) { MemberAccessor.SetNullableInt64   (o, value); }

		public virtual void SetNullableByte    (object o, Byte?     value) { MemberAccessor.SetNullableByte    (o, value); }
		[CLSCompliant(false)]
		public virtual void SetNullableUInt16  (object o, UInt16?   value) { MemberAccessor.SetNullableUInt16  (o, value); }
		[CLSCompliant(false)]
		public virtual void SetNullableUInt32  (object o, UInt32?   value) { MemberAccessor.SetNullableUInt32  (o, value); }
		[CLSCompliant(false)]
		public virtual void SetNullableUInt64  (object o, UInt64?   value) { MemberAccessor.SetNullableUInt64  (o, value); }

		public virtual void SetNullableBoolean (object o, Boolean?  value) { MemberAccessor.SetNullableBoolean (o, value); }
		public virtual void SetNullableChar    (object o, Char?     value) { MemberAccessor.SetNullableChar    (o, value); }
		public virtual void SetNullableSingle  (object o, Single?   value) { MemberAccessor.SetNullableSingle  (o, value); }
		public virtual void SetNullableDouble  (object o, Double?   value) { MemberAccessor.SetNullableDouble  (o, value); }
		public virtual void SetNullableDecimal (object o, Decimal?  value) { MemberAccessor.SetNullableDecimal (o, value); }
		public virtual void SetNullableGuid    (object o, Guid?     value) { MemberAccessor.SetNullableGuid    (o, value); }
		public virtual void SetNullableDateTime(object o, DateTime? value) { MemberAccessor.SetNullableDateTime(o, value); }
		public virtual void SetNullableDateTimeOffset(object o, DateTimeOffset? value) { MemberAccessor.SetNullableDateTimeOffset(o, value); }

#if !SILVERLIGHT

		// SQL type setters.
		//
		public virtual void SetSqlByte    (object o, SqlByte     value) { MemberAccessor.SetSqlByte    (o, value); }
		public virtual void SetSqlInt16   (object o, SqlInt16    value) { MemberAccessor.SetSqlInt16   (o, value); }
		public virtual void SetSqlInt32   (object o, SqlInt32    value) { MemberAccessor.SetSqlInt32   (o, value); }
		public virtual void SetSqlInt64   (object o, SqlInt64    value) { MemberAccessor.SetSqlInt64   (o, value); }
		public virtual void SetSqlSingle  (object o, SqlSingle   value) { MemberAccessor.SetSqlSingle  (o, value); }
		public virtual void SetSqlBoolean (object o, SqlBoolean  value) { MemberAccessor.SetSqlBoolean (o, value); }
		public virtual void SetSqlDouble  (object o, SqlDouble   value) { MemberAccessor.SetSqlDouble  (o, value); }
		public virtual void SetSqlDateTime(object o, SqlDateTime value) { MemberAccessor.SetSqlDateTime(o, value); }
		public virtual void SetSqlDecimal (object o, SqlDecimal  value) { MemberAccessor.SetSqlDecimal (o, value); }
		public virtual void SetSqlMoney   (object o, SqlMoney    value) { MemberAccessor.SetSqlMoney   (o, value); }
		public virtual void SetSqlGuid    (object o, SqlGuid     value) { MemberAccessor.SetSqlGuid    (o, value); }
		public virtual void SetSqlString  (object o, SqlString   value) { MemberAccessor.SetSqlString  (o, value); }

#endif

		public virtual void CloneValue    (object source, object dest)  { MemberAccessor.CloneValue(source, dest); }

		#endregion

		#region Intermal Mappers

		#region Complex Mapper

		internal sealed class ComplexMapper : MemberMapper
		{
			public ComplexMapper(MemberMapper memberMapper)
			{
				_mapper = memberMapper;
			}

			private readonly MemberMapper _mapper;

			object GetObject(object o)
			{
				return MemberAccessor.GetValue(o);
			}

			#region GetValue

			public override object GetValue(object o)
			{
				var obj = MemberAccessor.GetValue(o);
				return obj == null? null: _mapper.GetValue(obj);
			}

			// Simple type getters.
			//
			public override SByte    GetSByte   (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultSByteNullValue:    _mapper.GetSByte   (obj); }
			public override Int16    GetInt16   (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultInt16NullValue:    _mapper.GetInt16   (obj); }
			public override Int32    GetInt32   (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultInt32NullValue:    _mapper.GetInt32   (obj); }
			public override Int64    GetInt64   (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultInt64NullValue:    _mapper.GetInt64   (obj); }

			public override Byte     GetByte    (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultByteNullValue:     _mapper.GetByte    (obj); }
			public override UInt16   GetUInt16  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultUInt16NullValue:   _mapper.GetUInt16  (obj); }
			public override UInt32   GetUInt32  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultUInt32NullValue:   _mapper.GetUInt32  (obj); }
			public override UInt64   GetUInt64  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultUInt64NullValue:   _mapper.GetUInt64  (obj); }

			public override Boolean  GetBoolean (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultBooleanNullValue:  _mapper.GetBoolean (obj); }
			public override Char     GetChar    (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultCharNullValue:     _mapper.GetChar    (obj); }
			public override Single   GetSingle  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultSingleNullValue:   _mapper.GetSingle  (obj); }
			public override Double   GetDouble  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultDoubleNullValue:   _mapper.GetDouble  (obj); }
			public override Decimal  GetDecimal (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultDecimalNullValue:  _mapper.GetDecimal (obj); }
			public override Guid     GetGuid    (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultGuidNullValue:     _mapper.GetGuid    (obj); }
			public override DateTime GetDateTime(object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultDateTimeNullValue: _mapper.GetDateTime(obj); }
			public override DateTimeOffset GetDateTimeOffset(object o) { var obj = MemberAccessor.GetValue(o); return obj == null? MappingSchema.DefaultDateTimeOffsetNullValue: _mapper.GetDateTimeOffset(obj); }

			// Nullable type getters.
			//
			public override SByte?    GetNullableSByte   (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableSByte   (obj); }
			public override Int16?    GetNullableInt16   (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableInt16   (obj); }
			public override Int32?    GetNullableInt32   (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableInt32   (obj); }
			public override Int64?    GetNullableInt64   (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableInt64   (obj); }

			public override Byte?     GetNullableByte    (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableByte    (obj); }
			public override UInt16?   GetNullableUInt16  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableUInt16  (obj); }
			public override UInt32?   GetNullableUInt32  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableUInt32  (obj); }
			public override UInt64?   GetNullableUInt64  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableUInt64  (obj); }

			public override Boolean?  GetNullableBoolean (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableBoolean (obj); }
			public override Char?     GetNullableChar    (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableChar    (obj); }
			public override Single?   GetNullableSingle  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableSingle  (obj); }
			public override Double?   GetNullableDouble  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableDouble  (obj); }
			public override Decimal?  GetNullableDecimal (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableDecimal (obj); }
			public override Guid?     GetNullableGuid    (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableGuid    (obj); }
			public override DateTime? GetNullableDateTime(object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableDateTime(obj); }
			public override DateTimeOffset? GetNullableDateTimeOffset(object o) { var obj = MemberAccessor.GetValue(o); return obj == null? null: _mapper.GetNullableDateTimeOffset(obj); }

#if !SILVERLIGHT

			// SQL type getters.
			//
			public override SqlByte     GetSqlByte    (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? SqlByte.    Null: _mapper.GetSqlByte    (obj); }
			public override SqlInt16    GetSqlInt16   (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? SqlInt16.   Null: _mapper.GetSqlInt16   (obj); }
			public override SqlInt32    GetSqlInt32   (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? SqlInt32.   Null: _mapper.GetSqlInt32   (obj); }
			public override SqlInt64    GetSqlInt64   (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? SqlInt64.   Null: _mapper.GetSqlInt64   (obj); }
			public override SqlSingle   GetSqlSingle  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? SqlSingle.  Null: _mapper.GetSqlSingle  (obj); }
			public override SqlBoolean  GetSqlBoolean (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? SqlBoolean. Null: _mapper.GetSqlBoolean (obj); }
			public override SqlDouble   GetSqlDouble  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? SqlDouble.  Null: _mapper.GetSqlDouble  (obj); }
			public override SqlDateTime GetSqlDateTime(object o) { var obj = MemberAccessor.GetValue(o); return obj == null? SqlDateTime.Null: _mapper.GetSqlDateTime(obj); }
			public override SqlDecimal  GetSqlDecimal (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? SqlDecimal. Null: _mapper.GetSqlDecimal (obj); }
			public override SqlMoney    GetSqlMoney   (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? SqlMoney.   Null: _mapper.GetSqlMoney   (obj); }
			public override SqlGuid     GetSqlGuid    (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? SqlGuid.    Null: _mapper.GetSqlGuid    (obj); }
			public override SqlString   GetSqlString  (object o) { var obj = MemberAccessor.GetValue(o); return obj == null? SqlString.  Null: _mapper.GetSqlString  (obj); }

#endif

			#endregion

			#region SetValue

			public override void SetValue(object o, object value)
			{
				var obj = MemberAccessor.GetValue(o);

				if (obj != null)
					_mapper.SetValue(obj, value);
			}

			public override void SetSByte   (object o, SByte    value) { var obj = GetObject(o); if (obj != null) _mapper.SetSByte   (obj, value); }
			public override void SetInt16   (object o, Int16    value) { var obj = GetObject(o); if (obj != null) _mapper.SetInt16   (obj, value); }
			public override void SetInt32   (object o, Int32    value) { var obj = GetObject(o); if (obj != null) _mapper.SetInt32   (obj, value); }
			public override void SetInt64   (object o, Int64    value) { var obj = GetObject(o); if (obj != null) _mapper.SetInt64   (obj, value); }

			public override void SetByte    (object o, Byte     value) { var obj = GetObject(o); if (obj != null) _mapper.SetByte    (obj, value); }
			public override void SetUInt16  (object o, UInt16   value) { var obj = GetObject(o); if (obj != null) _mapper.SetUInt16  (obj, value); }
			public override void SetUInt32  (object o, UInt32   value) { var obj = GetObject(o); if (obj != null) _mapper.SetUInt32  (obj, value); }
			public override void SetUInt64  (object o, UInt64   value) { var obj = GetObject(o); if (obj != null) _mapper.SetUInt64  (obj, value); }

			public override void SetBoolean (object o, Boolean  value) { var obj = GetObject(o); if (obj != null) _mapper.SetBoolean (obj, value); }
			public override void SetChar    (object o, Char     value) { var obj = GetObject(o); if (obj != null) _mapper.SetChar    (obj, value); }
			public override void SetSingle  (object o, Single   value) { var obj = GetObject(o); if (obj != null) _mapper.SetSingle  (obj, value); }
			public override void SetDouble  (object o, Double   value) { var obj = GetObject(o); if (obj != null) _mapper.SetDouble  (obj, value); }
			public override void SetDecimal (object o, Decimal  value) { var obj = GetObject(o); if (obj != null) _mapper.SetDecimal (obj, value); }
			public override void SetGuid    (object o, Guid     value) { var obj = GetObject(o); if (obj != null) _mapper.SetGuid    (obj, value); }
			public override void SetDateTime(object o, DateTime value) { var obj = GetObject(o); if (obj != null) _mapper.SetDateTime(obj, value); }
			public override void SetDateTimeOffset(object o, DateTimeOffset value) { var obj = GetObject(o); if (obj != null) _mapper.SetDateTimeOffset(obj, value); }

			// Nullable type setters.
			//
			public override void SetNullableSByte   (object o, SByte?    value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableSByte   (obj, value); }
			public override void SetNullableInt16   (object o, Int16?    value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableInt16   (obj, value); }
			public override void SetNullableInt32   (object o, Int32?    value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableInt32   (obj, value); }
			public override void SetNullableInt64   (object o, Int64?    value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableInt64   (obj, value); }

			public override void SetNullableByte    (object o, Byte?     value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableByte    (obj, value); }
			public override void SetNullableUInt16  (object o, UInt16?   value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableUInt16  (obj, value); }
			public override void SetNullableUInt32  (object o, UInt32?   value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableUInt32  (obj, value); }
			public override void SetNullableUInt64  (object o, UInt64?   value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableUInt64  (obj, value); }

			public override void SetNullableBoolean (object o, Boolean?  value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableBoolean (obj, value); }
			public override void SetNullableChar    (object o, Char?     value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableChar    (obj, value); }
			public override void SetNullableSingle  (object o, Single?   value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableSingle  (obj, value); }
			public override void SetNullableDouble  (object o, Double?   value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableDouble  (obj, value); }
			public override void SetNullableDecimal (object o, Decimal?  value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableDecimal (obj, value); }
			public override void SetNullableGuid    (object o, Guid?     value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableGuid    (obj, value); }
			public override void SetNullableDateTime(object o, DateTime? value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableDateTime(obj, value); }
			public override void SetNullableDateTimeOffset(object o, DateTimeOffset? value) { var obj = GetObject(o); if (obj != null) _mapper.SetNullableDateTimeOffset(obj, value); }

#if !SILVERLIGHT

			// SQL type setters.
			//
			public override void SetSqlByte    (object o, SqlByte     value) { var obj = GetObject(o); if (obj != null) _mapper.SetSqlByte    (obj, value); }
			public override void SetSqlInt16   (object o, SqlInt16    value) { var obj = GetObject(o); if (obj != null) _mapper.SetSqlInt16   (obj, value); }
			public override void SetSqlInt32   (object o, SqlInt32    value) { var obj = GetObject(o); if (obj != null) _mapper.SetSqlInt32   (obj, value); }
			public override void SetSqlInt64   (object o, SqlInt64    value) { var obj = GetObject(o); if (obj != null) _mapper.SetSqlInt64   (obj, value); }
			public override void SetSqlSingle  (object o, SqlSingle   value) { var obj = GetObject(o); if (obj != null) _mapper.SetSqlSingle  (obj, value); }
			public override void SetSqlBoolean (object o, SqlBoolean  value) { var obj = GetObject(o); if (obj != null) _mapper.SetSqlBoolean (obj, value); }
			public override void SetSqlDouble  (object o, SqlDouble   value) { var obj = GetObject(o); if (obj != null) _mapper.SetSqlDouble  (obj, value); }
			public override void SetSqlDateTime(object o, SqlDateTime value) { var obj = GetObject(o); if (obj != null) _mapper.SetSqlDateTime(obj, value); }
			public override void SetSqlDecimal (object o, SqlDecimal  value) { var obj = GetObject(o); if (obj != null) _mapper.SetSqlDecimal (obj, value); }
			public override void SetSqlMoney   (object o, SqlMoney    value) { var obj = GetObject(o); if (obj != null) _mapper.SetSqlMoney   (obj, value); }
			public override void SetSqlGuid    (object o, SqlGuid     value) { var obj = GetObject(o); if (obj != null) _mapper.SetSqlGuid    (obj, value); }
			public override void SetSqlString  (object o, SqlString   value) { var obj = GetObject(o); if (obj != null) _mapper.SetSqlString  (obj, value); }

#endif

			#endregion
		}

		#endregion

		#region Primitive Mappers

		private static MemberMapper GetPrimitiveMemberMapper(MapMemberInfo mi)
		{
			if (mi.MapValues != null)
				return null;

			var n    = mi.Nullable;
			var type = mi.MemberAccessor.UnderlyingType;
 
			if (type == typeof(SByte))   return n? new SByteMapper.  Nullable(): new SByteMapper();
			if (type == typeof(Int16))   return n? new Int16Mapper.  Nullable(): new Int16Mapper();
			if (type == typeof(Int32))   return n? new Int32Mapper.  Nullable(): new Int32Mapper();
			if (type == typeof(Int64))   return n? new Int64Mapper.  Nullable(): new Int64Mapper();
			if (type == typeof(Byte))    return n? new ByteMapper.   Nullable(): new ByteMapper();
			if (type == typeof(UInt16))  return n? new UInt16Mapper. Nullable(): new UInt16Mapper();
			if (type == typeof(UInt32))  return n? new UInt32Mapper. Nullable(): new UInt32Mapper();
			if (type == typeof(UInt64))  return n? new UInt64Mapper. Nullable(): new UInt64Mapper();
			if (type == typeof(Single))  return n? new SingleMapper. Nullable(): new SingleMapper();
			if (type == typeof(Double))  return n? new DoubleMapper. Nullable(): new DoubleMapper();
			if (type == typeof(Char))    return n? new CharMapper.   Nullable(): new CharMapper();
			if (type == typeof(Boolean)) return n? new BooleanMapper.Nullable(): new BooleanMapper();

			throw new InvalidOperationException();
		}

		#endregion

		#region Simple Mappers

		private static MemberMapper GetSimpleMemberMapper(MapMemberInfo mi)
		{
			if (mi.MapValues != null)
				return null;

			var n    = mi.Nullable;
			var type = mi.Type;

			if (type == typeof(String))
				if (mi.Trimmable) return n? new StringMapper.Trimmable.NullableT(): new StringMapper.Trimmable();
				else              return n? new StringMapper.Nullable()           : new StringMapper();

			if (type == typeof(DateTime))       return n? new DateTimeMapper.Nullable()       : new DateTimeMapper();
			if (type == typeof(DateTimeOffset)) return n? new DateTimeOffsetMapper.Nullable() : new DateTimeOffsetMapper();
			if (type == typeof(Decimal))        return n? new DecimalMapper.Nullable()        : new DecimalMapper();
			if (type == typeof(Guid))           return n? new GuidMapper.Nullable()           : new GuidMapper();
			if (type == typeof(Stream))         return n? new StreamMapper.Nullable()         : new StreamMapper();
#if !SILVERLIGHT
			if (type == typeof(XmlReader))      return n? new XmlReaderMapper.Nullable()      : new XmlReaderMapper();
			if (type == typeof(XmlDocument))    return n? new XmlDocumentMapper.Nullable()    : new XmlDocumentMapper();
#endif
			return null;
		}

		class StringMapper : MemberMapper
		{
			string _nullValue;

			public override void SetValue(object o, object value)
			{
				MemberAccessor.SetValue(
					o,
					value is string? value:
					value == null?   _nullValue:
					                 MappingSchema.ConvertToString(value));
			}

			public override void Init(MapMemberInfo mapMemberInfo)
			{
				if (mapMemberInfo == null) throw new ArgumentNullException("mapMemberInfo");

				if (mapMemberInfo.NullValue != null)
					_nullValue = Convert.ToString(mapMemberInfo.NullValue);

				base.Init(mapMemberInfo);
			}

			public class Nullable : StringMapper
			{
				public override object GetValue(object o)
				{
					var value = MemberAccessor.GetValue(o);
					return (string)value == _nullValue? null: value;
				}
			}

			public class Trimmable : StringMapper
			{
				public override void SetValue(object o, object value)
				{
					MemberAccessor.SetValue(
						o, value == null? _nullValue: MappingSchema.ConvertToString(value).TrimEnd(_trim));
				}

				public class NullableT : Trimmable
				{
					public override object GetValue(object o)
					{
						var value = MemberAccessor.GetValue(o);
						return (string)value == _nullValue? null: value;
					}
				}
			}
		}

		#endregion

		#region Nullable Mappers

		private static MemberMapper GetNullableMemberMapper(MapMemberInfo mi)
		{
			var type = mi.Type;

			if (type.IsGenericType == false || mi.MapValues != null)
				return null;

			var underlyingType = Nullable.GetUnderlyingType(type);

			if (underlyingType == null)
				return null;

			if (underlyingType.IsEnum)
			{
				underlyingType = Enum.GetUnderlyingType(underlyingType);

				if (underlyingType == typeof(SByte))    return new NullableSByteMapper. Enum();
				if (underlyingType == typeof(Int16))    return new NullableInt16Mapper. Enum();
				if (underlyingType == typeof(Int32))    return new NullableInt32Mapper. Enum();
				if (underlyingType == typeof(Int64))    return new NullableInt64Mapper. Enum();
				if (underlyingType == typeof(Byte))     return new NullableByteMapper.  Enum();
				if (underlyingType == typeof(UInt16))   return new NullableUInt16Mapper.Enum();
				if (underlyingType == typeof(UInt32))   return new NullableUInt32Mapper.Enum();
				if (underlyingType == typeof(UInt64))   return new NullableUInt64Mapper.Enum();
			}
			else
			{
				if (underlyingType == typeof(SByte))    return new NullableSByteMapper();
				if (underlyingType == typeof(Int16))    return new NullableInt16Mapper();
				if (underlyingType == typeof(Int32))    return new NullableInt32Mapper();
				if (underlyingType == typeof(Int64))    return new NullableInt64Mapper();
				if (underlyingType == typeof(Byte))     return new NullableByteMapper();
				if (underlyingType == typeof(UInt16))   return new NullableUInt16Mapper();
				if (underlyingType == typeof(UInt32))   return new NullableUInt32Mapper();
				if (underlyingType == typeof(UInt64))   return new NullableUInt64Mapper();
				if (underlyingType == typeof(Char))     return new NullableCharMapper();
				if (underlyingType == typeof(Single))   return new NullableSingleMapper();
				if (underlyingType == typeof(Boolean))  return new NullableBooleanMapper();
				if (underlyingType == typeof(Double))   return new NullableDoubleMapper();
				if (underlyingType == typeof(DateTime)) return new NullableDateTimeMapper();
				if (underlyingType == typeof(Decimal))  return new NullableDecimalMapper();
				if (underlyingType == typeof(Guid))     return new NullableGuidMapper();
			}

			return null;
		}

		abstract class NullableEnumMapper : MemberMapper
		{
			protected Type MemberType;
			protected Type UnderlyingType;

			public override void Init(MapMemberInfo mapMemberInfo)
			{
				if (mapMemberInfo == null) throw new ArgumentNullException("mapMemberInfo");

				MemberType     = Nullable.GetUnderlyingType(mapMemberInfo.Type);
				UnderlyingType = mapMemberInfo.MemberAccessor.UnderlyingType;

				base.Init(mapMemberInfo);
			}
		}

		#endregion

		#region SqlTypes

#if !SILVERLIGHT

		private static MemberMapper GetSqlTypeMemberMapper(MapMemberInfo mi)
		{
			var type = mi.Type;

			if (ReflectionExtensions.IsSameOrParent(typeof(INullable), type) == false)
				return null;

			var d = mi.MapValues != null;

			if (type == typeof(SqlByte))     return d? new SqlByteMapper.    Default(): new SqlByteMapper();
			if (type == typeof(SqlInt16))    return d? new SqlInt16Mapper.   Default(): new SqlInt16Mapper();
			if (type == typeof(SqlInt32))    return d? new SqlInt32Mapper.   Default(): new SqlInt32Mapper();
			if (type == typeof(SqlInt64))    return d? new SqlInt64Mapper.   Default(): new SqlInt64Mapper();
			if (type == typeof(SqlSingle))   return d? new SqlSingleMapper.  Default(): new SqlSingleMapper();
			if (type == typeof(SqlBoolean))  return d? new SqlBooleanMapper. Default(): new SqlBooleanMapper();
			if (type == typeof(SqlDouble))   return d? new SqlDoubleMapper.  Default(): new SqlDoubleMapper();
			if (type == typeof(SqlDateTime)) return d? new SqlDateTimeMapper.Default(): new SqlDateTimeMapper();
			if (type == typeof(SqlDecimal))  return d? new SqlDecimalMapper. Default(): new SqlDecimalMapper();
			if (type == typeof(SqlMoney))    return d? new SqlMoneyMapper.   Default(): new SqlMoneyMapper();
			if (type == typeof(SqlGuid))     return d? new SqlGuidMapper.    Default(): new SqlGuidMapper();
			if (type == typeof(SqlString))   return d? new SqlStringMapper.  Default(): new SqlStringMapper();

			return null;
		}

#endif

		#endregion

		#endregion

		#region MapFrom, MapTo

		protected object MapFrom(object value)
		{
			return MapFrom(value, MapMemberInfo);
		}

		static readonly char[] _trim = { ' ' };

		protected object MapFrom(object value, MapMemberInfo mapInfo)
		{
			if (mapInfo == null) throw new ArgumentNullException("mapInfo");

			if (value == null)
				return mapInfo.NullValue;

			if (mapInfo.Trimmable && value is string)
				value = value.ToString().TrimEnd(_trim);

			if (mapInfo.MapValues != null)
			{
				var comp = (IComparable)value;

				foreach (var mv       in mapInfo.MapValues)
				foreach (var mapValue in mv.MapValues)
				{
					try
					{
						if (comp is string && ((string)comp).Length == 1 && mapValue is char)
						{
							if (((string)comp)[0] == (char)mapValue)
								return mv.OrigValue;
						}
						else if (comp.CompareTo(mapValue) == 0)
							return mv.OrigValue;
					}
					catch
					{
					}
				}
			}

			var valueType  = value.GetType();
			var memberType = mapInfo.Type;

			if (!ReflectionExtensions.IsSameOrParent(memberType, valueType))
			{
				if (memberType.IsGenericType)
				{
					var underlyingType = Nullable.GetUnderlyingType(memberType);

					if (valueType == underlyingType)
						return value;

					memberType = underlyingType;
				}

				if (memberType.IsEnum)
				{
					var underlyingType = mapInfo.MemberAccessor.UnderlyingType;

					if (valueType != underlyingType)
						//value = _mappingSchema.ConvertChangeType(value, underlyingType);
						return MapFrom(MappingSchema.ConvertChangeType(value, underlyingType), mapInfo);

					//value = Enum.Parse(type, Enum.GetName(type, value));
					value = Enum.ToObject(memberType, value);
				}
				else
				{
					value = MappingSchema.ConvertChangeType(value, memberType);
				}
			}

			return value;
		}

		protected object MapTo(object value)
		{
			return MapTo(value, MapMemberInfo);
		}

		protected static object MapTo(object value, MapMemberInfo mapInfo)
		{
			if (mapInfo == null) throw new ArgumentNullException("mapInfo");

			if (value == null)
				return null;

			if (mapInfo.Nullable && mapInfo.NullValue != null)
			{
				try
				{
					var comp = (IComparable)value;

					if (comp.CompareTo(mapInfo.NullValue) == 0)
						return null;
				}
				catch
				{
				}
			}

			if (mapInfo.MapValues != null)
			{
				var comp = (IComparable)value;

				foreach (var mv in mapInfo.MapValues)
				{
					try
					{
						if (comp.CompareTo(mv.OrigValue) == 0)
							return mv.MapValues[0];
					}
					catch
					{
					}
				}
			}

			return value;
		}

		#endregion
	}
}
