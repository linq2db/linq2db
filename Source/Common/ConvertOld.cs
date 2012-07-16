using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.IO;
using System.Threading;
using System.Xml;

namespace LinqToDB.Common
{
	using Properties;

	/// <summary>Converts a base data type to another base data type.</summary>
	public static partial class ConvertOld
	{
		#region Boolean

		/// <summary>Converts the value from <c>Char</c> to an equivalent <c>Boolean</c> value.</summary>
		public static Boolean ToBoolean(Char p)
		{
			switch (p)
			{
				case '\x0' : // Allow int <=> Char <=> Boolean
				case   '0' :
				case   'n' :
				case   'N' :
				case   'f' :
				case   'F' : return false;

				case '\x1' : // Allow int <=> Char <=> Boolean
				case   '1' :
				case   'y' :
				case   'Y' :
				case   't' :
				case   'T' : return true;
			}

			throw CreateInvalidCastException(typeof(Char), typeof(Boolean));
		}

		#endregion

		#region Byte[]

		/// <summary>Converts the value from <c>Decimal</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Byte[] ToByteArray(Decimal p)
		{
			var bits  = Decimal.GetBits(p);
			var bytes = new Byte[Buffer.ByteLength(bits)];

			Buffer.BlockCopy(bits, 0, bytes, 0, bytes.Length);
			return bytes;
		}

		/// <summary>Converts the value from <c>Stream</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Byte[] ToByteArray(Stream p)
		{
			if (p == null || object.ReferenceEquals(p, Stream.Null)) return null;
			if (p is MemoryStream)             return ((MemoryStream)p).ToArray();

			var position = p.Seek(0, SeekOrigin.Begin);
			var bytes    = new Byte[(int)p.Length];

			p.Read(bytes, 0, bytes.Length);
			p.Position = position;

			return bytes;
		}

		/// <summary>Converts the value from <c>Char[]</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Byte[] ToByteArray(Char[] p)
		{
			var bytes = new Byte[Buffer.ByteLength(p)];

			Buffer.BlockCopy(p, 0, bytes, 0, bytes.Length);
			return bytes;
		}

		#endregion

		#region Decimal

		/// <summary>Converts the value from <c>Byte[]</c> to an equivalent <c>Decimal</c> value.</summary>
		public static Decimal ToDecimal(Byte[] p)
		{
			if (p == null || p.Length == 0) return 0.0m;

			var bits = new int[p.Length / sizeof(int)];

			Buffer.BlockCopy(p, 0, bits, 0, p.Length);
			return new Decimal(bits);
		}

		public static Decimal ToDecimal(Binary p)
		{
			if (p == null || p.Length == 0) return 0.0m;

			var bits = new int[p.Length / sizeof(int)];

			Buffer.BlockCopy(p.ToArray(), 0, bits, 0, p.Length);
			return new Decimal(bits);
		}

		#endregion

		#region SqlTypes

#if !SILVERLIGHT

		#region SqlChars

		// Scalar Types.
		// 
		/// <summary>Converts the value from <c>String</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(String p)          { return p == null? SqlChars.Null: new SqlChars(p.ToCharArray()); }
		/// <summary>Converts the value from <c>Char[]</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Char[] p)          { return p == null? SqlChars.Null: new SqlChars(p); }
		/// <summary>Converts the value from <c>Byte[]</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Byte[] p)          { return p == null? SqlChars.Null: new SqlChars(ToCharArray(p)); }
		public static SqlChars ToSqlChars(Binary p)          { return p == null? SqlChars.Null: new SqlChars(ToCharArray(p.ToArray())); }

		/// <summary>Converts the value from <c>SByte</c> to an equivalent <c>SqlChars</c> value.</summary>
		[CLSCompliant(false)]
		public static SqlChars ToSqlChars(SByte p)           { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>Int16</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Int16 p)           { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>Int32</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Int32 p)           { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>Int64</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Int64 p)           { return new SqlChars(ToString(p).ToCharArray()); }

		/// <summary>Converts the value from <c>Byte</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Byte p)            { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>UInt16</c> to an equivalent <c>SqlChars</c> value.</summary>
		[CLSCompliant(false)]
		public static SqlChars ToSqlChars(UInt16 p)          { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>UInt32</c> to an equivalent <c>SqlChars</c> value.</summary>
		[CLSCompliant(false)]
		public static SqlChars ToSqlChars(UInt32 p)          { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>UInt64</c> to an equivalent <c>SqlChars</c> value.</summary>
		[CLSCompliant(false)]
		public static SqlChars ToSqlChars(UInt64 p)          { return new SqlChars(ToString(p).ToCharArray()); }

		/// <summary>Converts the value from <c>Single</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Single p)          { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>Double</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Double p)          { return new SqlChars(ToString(p).ToCharArray()); }

		/// <summary>Converts the value from <c>Boolean</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Boolean p)         { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>Decimal</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Decimal p)         { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>Char</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Char p)            { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>TimeSpan</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(TimeSpan p)        { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>DateTime</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(DateTime p)        { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>DateTimeOffset</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(DateTimeOffset p)  { return new SqlChars(ToString(p).ToCharArray()); }
		/// <summary>Converts the value from <c>Guid</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Guid p)            { return new SqlChars(ToString(p).ToCharArray()); }

		// Nullable Types.
		// 
		/// <summary>Converts the value from <c>SByte?</c> to an equivalent <c>SqlChars</c> value.</summary>
		[CLSCompliant(false)]
		public static SqlChars ToSqlChars(SByte? p)          { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }
		/// <summary>Converts the value from <c>Int16?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Int16? p)          { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }
		/// <summary>Converts the value from <c>Int32?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Int32? p)          { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }
		/// <summary>Converts the value from <c>Int64?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Int64? p)          { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }

		/// <summary>Converts the value from <c>Byte?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Byte? p)           { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }
		/// <summary>Converts the value from <c>UInt16?</c> to an equivalent <c>SqlChars</c> value.</summary>
		[CLSCompliant(false)]
		public static SqlChars ToSqlChars(UInt16? p)         { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }
		/// <summary>Converts the value from <c>UInt32?</c> to an equivalent <c>SqlChars</c> value.</summary>
		[CLSCompliant(false)]
		public static SqlChars ToSqlChars(UInt32? p)         { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }
		/// <summary>Converts the value from <c>UInt64?</c> to an equivalent <c>SqlChars</c> value.</summary>
		[CLSCompliant(false)]
		public static SqlChars ToSqlChars(UInt64? p)         { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }

		/// <summary>Converts the value from <c>Single?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Single? p)         { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }
		/// <summary>Converts the value from <c>Double?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Double? p)         { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }

		/// <summary>Converts the value from <c>Boolean?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Boolean? p)        { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }
		/// <summary>Converts the value from <c>Decimal?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Decimal? p)        { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }
		/// <summary>Converts the value from <c>Char?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Char? p)           { return p.HasValue? new SqlChars(new Char[]{p.Value})       : SqlChars.Null; }
		/// <summary>Converts the value from <c>TimeSpan?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(TimeSpan? p)       { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }
		/// <summary>Converts the value from <c>DateTime?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(DateTime? p)       { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }
		/// <summary>Converts the value from <c>DateTimeOffset?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(DateTimeOffset? p) { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }
		/// <summary>Converts the value from <c>Guid?</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Guid? p)           { return p.HasValue? new SqlChars(p.ToString().ToCharArray()): SqlChars.Null; }

		// SqlTypes
		// 
		/// <summary>Converts the value from <c>SqlString</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlString p)       { return (SqlChars)p; }

		/// <summary>Converts the value from <c>SqlByte</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlByte p)         { return (SqlChars)p.ToSqlString(); }
		/// <summary>Converts the value from <c>SqlInt16</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlInt16 p)        { return (SqlChars)p.ToSqlString(); }
		/// <summary>Converts the value from <c>SqlInt32</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlInt32 p)        { return (SqlChars)p.ToSqlString(); }
		/// <summary>Converts the value from <c>SqlInt64</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlInt64 p)        { return (SqlChars)p.ToSqlString(); }

		/// <summary>Converts the value from <c>SqlSingle</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlSingle p)       { return (SqlChars)p.ToSqlString(); }
		/// <summary>Converts the value from <c>SqlDouble</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlDouble p)       { return (SqlChars)p.ToSqlString(); }
		/// <summary>Converts the value from <c>SqlDecimal</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlDecimal p)      { return (SqlChars)p.ToSqlString(); }
		/// <summary>Converts the value from <c>SqlMoney</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlMoney p)        { return (SqlChars)p.ToSqlString(); }

		/// <summary>Converts the value from <c>SqlBoolean</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlBoolean p)      { return (SqlChars)p.ToSqlString(); }
		/// <summary>Converts the value from <c>SqlGuid</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlGuid p)         { return (SqlChars)p.ToSqlString(); }
		/// <summary>Converts the value from <c>SqlDateTime</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlDateTime p)     { return (SqlChars)p.ToSqlString(); }
		/// <summary>Converts the value from <c>SqlBinary</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(SqlBinary p)       { return p.IsNull? SqlChars.Null: new SqlChars(p.ToString().ToCharArray()); }

		/// <summary>Converts the value from <c>Type</c> to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(Type p)            { return p == null? SqlChars.Null: new SqlChars(p.FullName.ToCharArray()); }
		/// <summary>Converts the value of a specified object to an equivalent <c>SqlChars</c> value.</summary>
		public static SqlChars ToSqlChars(object p)         
		{
			if (p == null || p is DBNull) return SqlChars.Null;

			if (p is SqlChars) return (SqlChars)p;

			// Scalar Types.
			//
			if (p is String)          return ToSqlChars((String)p);
			if (p is Char[])          return ToSqlChars((Char[])p);
			if (p is Byte[])          return ToSqlChars((Byte[])p);
			if (p is Binary)          return ToSqlChars(((Binary)p).ToArray());

			if (p is SByte)           return ToSqlChars((SByte)p);
			if (p is Int16)           return ToSqlChars((Int16)p);
			if (p is Int32)           return ToSqlChars((Int32)p);
			if (p is Int64)           return ToSqlChars((Int64)p);

			if (p is Byte)            return ToSqlChars((Byte)p);
			if (p is UInt16)          return ToSqlChars((UInt16)p);
			if (p is UInt32)          return ToSqlChars((UInt32)p);
			if (p is UInt64)          return ToSqlChars((UInt64)p);

			if (p is Single)          return ToSqlChars((Single)p);
			if (p is Double)          return ToSqlChars((Double)p);

			if (p is Boolean)         return ToSqlChars((Boolean)p);
			if (p is Decimal)         return ToSqlChars((Decimal)p);

			// SqlTypes
			//
			if (p is SqlString)       return ToSqlChars((SqlString)p);

			if (p is SqlByte)         return ToSqlChars((SqlByte)p);
			if (p is SqlInt16)        return ToSqlChars((SqlInt16)p);
			if (p is SqlInt32)        return ToSqlChars((SqlInt32)p);
			if (p is SqlInt64)        return ToSqlChars((SqlInt64)p);

			if (p is SqlSingle)       return ToSqlChars((SqlSingle)p);
			if (p is SqlDouble)       return ToSqlChars((SqlDouble)p);
			if (p is SqlDecimal)      return ToSqlChars((SqlDecimal)p);
			if (p is SqlMoney)        return ToSqlChars((SqlMoney)p);

			if (p is SqlBoolean)      return ToSqlChars((SqlBoolean)p);
			if (p is SqlBinary)       return ToSqlChars((SqlBinary)p);
			if (p is Type)            return ToSqlChars((Type)p);

			return new SqlChars(ToString(p).ToCharArray());
		}

		#endregion

#endif

		#endregion

		#region Other Types

		#region Binary

		// Scalar Types.
		// 
		/// <summary>Converts the value from <c>String</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(String p)          { return p == null? null: new Binary(System.Text.Encoding.UTF8.GetBytes(p)); }
		/// <summary>Converts the value from <c>Byte</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Byte p)            { return new Binary(new byte[]{p}); }
		/// <summary>Converts the value from <c>SByte</c> to an equivalent <c>Byte[]</c> value.</summary>
		[CLSCompliant(false)]
		public static Binary ToLinqBinary(SByte p)           { return new Binary(new byte[]{checked((Byte)p)}); }
		/// <summary>Converts the value from <c>Decimal</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Decimal p)        
		{
			var bits  = Decimal.GetBits(p);
			var bytes = new Byte[Buffer.ByteLength(bits)];

			Buffer.BlockCopy(bits, 0, bytes, 0, bytes.Length);
			return new Binary(bytes);
		}
		/// <summary>Converts the value from <c>Int16</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Int16 p)           { return new Binary(BitConverter.GetBytes(p)); }
		/// <summary>Converts the value from <c>Int32</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Int32 p)           { return new Binary(BitConverter.GetBytes(p)); }
		/// <summary>Converts the value from <c>Int64</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Int64 p)           { return new Binary(BitConverter.GetBytes(p)); }

		/// <summary>Converts the value from <c>UInt16</c> to an equivalent <c>Byte[]</c> value.</summary>
		[CLSCompliant(false)]
		public static Binary ToLinqBinary(UInt16 p)          { return new Binary(BitConverter.GetBytes(p)); }
		/// <summary>Converts the value from <c>UInt32</c> to an equivalent <c>Byte[]</c> value.</summary>
		[CLSCompliant(false)]
		public static Binary ToLinqBinary(UInt32 p)          { return new Binary(BitConverter.GetBytes(p)); }
		/// <summary>Converts the value from <c>UInt64</c> to an equivalent <c>Byte[]</c> value.</summary>
		[CLSCompliant(false)]
		public static Binary ToLinqBinary(UInt64 p)          { return new Binary(BitConverter.GetBytes(p)); }

		/// <summary>Converts the value from <c>Single</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Single p)          { return new Binary(BitConverter.GetBytes(p)); }
		/// <summary>Converts the value from <c>Double</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Double p)          { return new Binary(BitConverter.GetBytes(p)); }

		/// <summary>Converts the value from <c>Boolean</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Boolean p)         { return new Binary(BitConverter.GetBytes(p)); }
		/// <summary>Converts the value from <c>Char</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Char p)            { return new Binary(BitConverter.GetBytes(p)); }
#if !SILVERLIGHT
		/// <summary>Converts the value from <c>DateTime</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(DateTime p)        { return new Binary(ToByteArray(p.ToBinary())); }
		/// <summary>Converts the value from <c>DateTimeOffset</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(DateTimeOffset p)  { return new Binary(ToByteArray(p.LocalDateTime.ToBinary())); }
#endif
		public static Binary ToLinqBinary(Byte[]         p)  { return new Binary(p); }
		/// <summary>Converts the value from <c>TimeSpan</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(TimeSpan p)        { return new Binary(ToByteArray(p.Ticks)); }
		/// <summary>Converts the value from <c>Stream</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Stream p)         
		{
			if (p == null)         return null;
			if (p is MemoryStream) return ((MemoryStream)p).ToArray();

			var position = p.Seek(0, SeekOrigin.Begin);
			var bytes = new Byte[(int)p.Length];
			p.Read(bytes, 0, bytes.Length);
			p.Position = position;

			return new Binary(bytes);
		}
		/// <summary>Converts the value from <c>Char[]</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Char[] p)
		{
			var bytes = new Byte[Buffer.ByteLength(p)];

			Buffer.BlockCopy(p, 0, bytes, 0, bytes.Length);
			return new Binary(bytes);
		}
		/// <summary>Converts the value from <c>Guid</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Guid p)            { return p == Guid.Empty? null: new Binary(p.ToByteArray()); }

		// Nullable Types.
		// 
		/// <summary>Converts the value from <c>SByte?</c> to an equivalent <c>Byte[]</c> value.</summary>
		[CLSCompliant(false)]
		public static Binary ToLinqBinary(SByte? p)          { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>Int16?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Int16? p)          { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>Int32?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Int32? p)          { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>Int64?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Int64? p)          { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }

		/// <summary>Converts the value from <c>Byte?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Byte? p)           { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>UInt16?</c> to an equivalent <c>Byte[]</c> value.</summary>
		[CLSCompliant(false)]
		public static Binary ToLinqBinary(UInt16? p)         { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>UInt32?</c> to an equivalent <c>Byte[]</c> value.</summary>
		[CLSCompliant(false)]
		public static Binary ToLinqBinary(UInt32? p)         { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>UInt64?</c> to an equivalent <c>Byte[]</c> value.</summary>
		[CLSCompliant(false)]
		public static Binary ToLinqBinary(UInt64? p)         { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }

		/// <summary>Converts the value from <c>Single?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Single? p)         { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>Double?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Double? p)         { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }

		/// <summary>Converts the value from <c>Boolean?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Boolean? p)        { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>Decimal?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Decimal? p)        { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>Char?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Char? p)           { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>DateTime?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(DateTime? p)       { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>DateTimeOffset?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(DateTimeOffset? p) { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>TimeSpan?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(TimeSpan? p)       { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }
		/// <summary>Converts the value from <c>Guid?</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(Guid? p)           { return p.HasValue? new Binary(ToByteArray(p.Value)): null; }

#if !SILVERLIGHT

		// SqlTypes
		// 
		/// <summary>Converts the value from <c>SqlBinary</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlBinary p)       { return p.IsNull? null: new Binary(p.Value); }
		/// <summary>Converts the value from <c>SqlBytes</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlBytes p)        { return p.IsNull? null: new Binary(p.Value); }
		/// <summary>Converts the value from <c>SqlGuid</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlGuid p)         { return p.IsNull? null: new Binary(p.ToByteArray()); }
		/// <summary>Converts the value from <c>SqlString</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlString p)       { return p.IsNull? null: new Binary(ToByteArray(p.Value)); }

		/// <summary>Converts the value from <c>SqlByte</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlByte p)         { return p.IsNull? null: new Binary(ToByteArray(p.Value)); }
		/// <summary>Converts the value from <c>SqlInt16</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlInt16 p)        { return p.IsNull? null: new Binary(ToByteArray(p.Value)); }
		/// <summary>Converts the value from <c>SqlInt32</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlInt32 p)        { return p.IsNull? null: new Binary(ToByteArray(p.Value)); }
		/// <summary>Converts the value from <c>SqlInt64</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlInt64 p)        { return p.IsNull? null: new Binary(ToByteArray(p.Value)); }

		/// <summary>Converts the value from <c>SqlSingle</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlSingle p)       { return p.IsNull? null: new Binary(ToByteArray(p.Value)); }
		/// <summary>Converts the value from <c>SqlDouble</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlDouble p)       { return p.IsNull? null: new Binary(ToByteArray(p.Value)); }
		/// <summary>Converts the value from <c>SqlDecimal</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlDecimal p)      { return p.IsNull? null: new Binary(ToByteArray(p.Value)); }
		/// <summary>Converts the value from <c>SqlMoney</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlMoney p)        { return p.IsNull? null: new Binary(ToByteArray(p.Value)); }

		/// <summary>Converts the value from <c>SqlBoolean</c> to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(SqlBoolean p)      { return p.IsNull? null: new Binary(ToByteArray(p.Value)); }

#endif

		/// <summary>Converts the value of a specified object to an equivalent <c>Byte[]</c> value.</summary>
		public static Binary ToLinqBinary(object p)
		{
			if (p == null || p is DBNull) return null;

			if (p is Byte[]) return new Binary((Byte[])p);
			if (p is Binary) return (Binary)p;

			// Scalar Types.
			//
			if (p is String)          return ToLinqBinary((String)p);
			if (p is Byte)            return ToLinqBinary((Byte)p);
			if (p is SByte)           return ToLinqBinary((SByte)p);
			if (p is Decimal)         return ToLinqBinary((Decimal)p);
			if (p is Int16)           return ToLinqBinary((Int16)p);
			if (p is Int32)           return ToLinqBinary((Int32)p);
			if (p is Int64)           return ToLinqBinary((Int64)p);

			if (p is UInt16)          return ToLinqBinary((UInt16)p);
			if (p is UInt32)          return ToLinqBinary((UInt32)p);
			if (p is UInt64)          return ToLinqBinary((UInt64)p);

			if (p is Single)          return ToLinqBinary((Single)p);
			if (p is Double)          return ToLinqBinary((Double)p);

			if (p is Boolean)         return ToLinqBinary((Boolean)p);
			if (p is DateTime)        return ToLinqBinary((DateTime)p);
			if (p is DateTimeOffset)  return ToLinqBinary((DateTimeOffset)p);
			if (p is TimeSpan)        return ToLinqBinary((TimeSpan)p);
			if (p is Stream)          return ToLinqBinary((Stream)p);
			if (p is Char[])          return ToLinqBinary((Char[])p);
			if (p is Guid)            return ToLinqBinary((Guid)p);

#if !SILVERLIGHT

			// SqlTypes
			//
			if (p is SqlBinary)       return ToLinqBinary((SqlBinary)p);
			if (p is SqlBytes)        return ToLinqBinary((SqlBytes)p);
			if (p is SqlGuid)         return ToLinqBinary((SqlGuid)p);
			if (p is SqlString)       return ToLinqBinary((SqlString)p);

			if (p is SqlByte)         return ToLinqBinary((SqlByte)p);
			if (p is SqlInt16)        return ToLinqBinary((SqlInt16)p);
			if (p is SqlInt32)        return ToLinqBinary((SqlInt32)p);
			if (p is SqlInt64)        return ToLinqBinary((SqlInt64)p);

			if (p is SqlSingle)       return ToLinqBinary((SqlSingle)p);
			if (p is SqlDouble)       return ToLinqBinary((SqlDouble)p);
			if (p is SqlDecimal)      return ToLinqBinary((SqlDecimal)p);
			if (p is SqlMoney)        return ToLinqBinary((SqlMoney)p);

			if (p is SqlBoolean)      return ToLinqBinary((SqlBoolean)p);

#endif

			throw CreateInvalidCastException(p.GetType(), typeof(Byte[]));
		}

		#endregion

		#region Char[]

		// Scalar Types.
		// 
		/// <summary>Converts the value from <c>String</c> to an equivalent <c>Char[]</c> value.</summary>
		public static Char[] ToCharArray(String p)          { return p == null? null: p.ToCharArray(); }

#if !SILVERLIGHT

		// SqlTypes
		// 
		/// <summary>Converts the value from <c>SqlString</c> to an equivalent <c>Char[]</c> value.</summary>
		public static Char[] ToCharArray(SqlString p)       { return p.IsNull? null: p.Value.ToCharArray(); }
		/// <summary>Converts the value from <c>SqlChars</c> to an equivalent <c>Char[]</c> value.</summary>
		public static Char[] ToCharArray(SqlChars p)        { return p.IsNull? null: p.Value; }

#endif

		/// <summary>Converts the value from <c>Byte[]</c> to an equivalent <c>Char[]</c> value.</summary>
		public static Char[] ToCharArray(Byte[] p)
		{
			if (p == null) return null;

			var chars = new Char[p.Length / sizeof(Char)];

			Buffer.BlockCopy(p, 0, chars, 0, p.Length);
			return chars;
		}

		public static Char[] ToCharArray(Binary p)
		{
			if (p == null) return null;

			var chars = new Char[p.Length / sizeof(Char)];

			Buffer.BlockCopy(p.ToArray(), 0, chars, 0, p.Length);
			return chars;
		}

		/// <summary>Converts the value of a specified object to an equivalent <c>Char[]</c> value.</summary>
		public static Char[] ToCharArray(object p)
		{
			if (p == null || p is DBNull) return null;

			if (p is Char[]) return (Char[])p;

			// Scalar Types.
			//
			if (p is String)          return ToCharArray((String)p);

#if !SILVERLIGHT

			// SqlTypes
			//
			if (p is SqlString)       return ToCharArray((SqlString)p);
			if (p is SqlChars)        return ToCharArray((SqlChars)p);

#endif
			if (p is Byte[])          return ToCharArray((Byte[])p);
			if (p is Binary)          return ToCharArray(((Binary)p).ToArray());

			return ToString(p).ToCharArray();
		}

		#endregion

		#region XmlReader

#if !SILVERLIGHT

		// Scalar Types.
		// 
		/// <summary>Converts the value from <c>String</c> to an equivalent <c>XmlReader</c> value.</summary>
		public static XmlReader ToXmlReader(String p)          { return p == null? null: new XmlTextReader(new StringReader(p)); }

		// SqlTypes
		// 
		/// <summary>Converts the value from <c>SqlXml</c> to an equivalent <c>XmlReader</c> value.</summary>
		public static XmlReader ToXmlReader(SqlXml p)          { return p.IsNull? null: p.CreateReader(); }
		/// <summary>Converts the value from <c>SqlString</c> to an equivalent <c>XmlReader</c> value.</summary>
		public static XmlReader ToXmlReader(SqlString p)       { return p.IsNull? null: new XmlTextReader(new StringReader(p.Value)); }
		/// <summary>Converts the value from <c>SqlChars</c> to an equivalent <c>XmlReader</c> value.</summary>
		public static XmlReader ToXmlReader(SqlChars p)        { return p.IsNull? null: new XmlTextReader(new StringReader(p.ToSqlString().Value)); }
		/// <summary>Converts the value from <c>SqlBinary</c> to an equivalent <c>XmlReader</c> value.</summary>
		public static XmlReader ToXmlReader(SqlBinary p)       { return p.IsNull? null: new XmlTextReader(new MemoryStream(p.Value)); }

		// Other Types.
		// 
		/// <summary>Converts the value from <c>Stream</c> to an equivalent <c>XmlReader</c> value.</summary>
		public static XmlReader ToXmlReader(Stream p)          { return p == null? null: new XmlTextReader(p); }
		/// <summary>Converts the value from <c>TextReader</c> to an equivalent <c>XmlReader</c> value.</summary>
		public static XmlReader ToXmlReader(TextReader p)      { return p == null? null: new XmlTextReader(p); }
		/// <summary>Converts the value from <c>XmlDocument</c> to an equivalent <c>XmlReader</c> value.</summary>
		public static XmlReader ToXmlReader(XmlDocument p)     { return p == null? null: new XmlTextReader(new StringReader(p.InnerXml)); }

		/// <summary>Converts the value from <c>Char[]</c> to an equivalent <c>XmlReader</c> value.</summary>
		public static XmlReader ToXmlReader(Char[] p)          { return p == null? null: new XmlTextReader(new StringReader(new string(p))); }
		/// <summary>Converts the value from <c>Byte[]</c> to an equivalent <c>XmlReader</c> value.</summary>
		public static XmlReader ToXmlReader(Byte[] p)          { return p == null? null: new XmlTextReader(new MemoryStream(p)); }
		public static XmlReader ToXmlReader(Binary p)          { return p == null? null: new XmlTextReader(new MemoryStream(p.ToArray())); }

		/// <summary>Converts the value of a specified object to an equivalent <c>XmlReader</c> value.</summary>
		public static XmlReader ToXmlReader(object p)         
		{
			if (p == null || p is DBNull) return null;

			if (p is XmlReader) return (XmlReader)p;

			// Scalar Types.
			//
			if (p is String)          return ToXmlReader((String)p);

			// SqlTypes
			//
			if (p is SqlXml)          return ToXmlReader((SqlXml)p);
			if (p is SqlString)       return ToXmlReader((SqlString)p);
			if (p is SqlChars)        return ToXmlReader((SqlChars)p);
			if (p is SqlBinary)       return ToXmlReader((SqlBinary)p);

			// Other Types.
			//
			if (p is XmlDocument)     return ToXmlReader((XmlDocument)p);

			if (p is Char[])          return ToXmlReader((Char[])p);
			if (p is Byte[])          return ToXmlReader((Byte[])p);
			if (p is Binary)          return ToXmlReader(((Binary)p).ToArray());

			throw CreateInvalidCastException(p.GetType(), typeof(XmlReader));
		}

#endif

		#endregion

		#region XmlDocument

#if !SILVERLIGHT

		// Scalar Types.
		// 
		/// <summary>Converts the value from <c>String</c> to an equivalent <c>XmlDocument</c> value.</summary>
		public static XmlDocument ToXmlDocument(String p)
		{
			if (string.IsNullOrEmpty(p)) return null;

			var doc = new XmlDocument();

			doc.LoadXml(p);

			return doc;
		}

		// SqlTypes
		// 
		/// <summary>Converts the value from <c>SqlString</c> to an equivalent <c>XmlDocument</c> value.</summary>
		public static XmlDocument ToXmlDocument(SqlString p)       { return p.IsNull? null: ToXmlDocument(p.Value); }
		/// <summary>Converts the value from <c>SqlXml</c> to an equivalent <c>XmlDocument</c> value.</summary>
		public static XmlDocument ToXmlDocument(SqlXml p)          { return p.IsNull? null: ToXmlDocument(p.Value); }
		/// <summary>Converts the value from <c>SqlChars</c> to an equivalent <c>XmlDocument</c> value.</summary>
		public static XmlDocument ToXmlDocument(SqlChars p)        { return p.IsNull? null: ToXmlDocument(p.ToSqlString().Value); }
		/// <summary>Converts the value from <c>SqlBinary</c> to an equivalent <c>XmlDocument</c> value.</summary>
		public static XmlDocument ToXmlDocument(SqlBinary p)       { return p.IsNull? null: ToXmlDocument(new MemoryStream(p.Value)); }

		// Other Types.
		// 
		/// <summary>Converts the value from <c>Stream</c> to an equivalent <c>XmlDocument</c> value.</summary>
		public static XmlDocument ToXmlDocument(Stream p)
		{
			if (p == null) return null;

			var doc = new XmlDocument();

			doc.Load(p);

			return doc;
		}

		/// <summary>Converts the value from <c>TextReader</c> to an equivalent <c>XmlDocument</c> value.</summary>
		public static XmlDocument ToXmlDocument(TextReader p)
		{
			if (p == null) return null;

			var doc = new XmlDocument();

			doc.Load(p);

			return doc;
		}

		/// <summary>Converts the value from <c>XmlReader</c> to an equivalent <c>XmlDocument</c> value.</summary>
		public static XmlDocument ToXmlDocument(XmlReader p)
		{
			if (p == null) return null;

			var doc = new XmlDocument();

			doc.Load(p);

			return doc;
		}

		/// <summary>Converts the value from <c>Char[]</c> to an equivalent <c>XmlDocument</c> value.</summary>
		public static XmlDocument ToXmlDocument(Char[] p)          { return p == null || p.Length == 0? null: ToXmlDocument(new string(p)); }
		/// <summary>Converts the value from <c>Byte[]</c> to an equivalent <c>XmlDocument</c> value.</summary>
		public static XmlDocument ToXmlDocument(Byte[] p)          { return p == null || p.Length == 0? null: ToXmlDocument(new MemoryStream(p)); }
		public static XmlDocument ToXmlDocument(Binary p)          { return p == null || p.Length == 0? null: ToXmlDocument(new MemoryStream(p.ToArray())); }

		/// <summary>Converts the value of a specified object to an equivalent <c>XmlDocument</c> value.</summary>
		public static XmlDocument ToXmlDocument(object p)         
		{
			if (p == null || p is DBNull) return null;

			if (p is XmlDocument) return (XmlDocument)p;

			// Scalar Types.
			//
			if (p is String)          return ToXmlDocument((String)p);

			// SqlTypes
			//
			if (p is SqlChars)        return ToXmlDocument((SqlChars)p);
			if (p is SqlBinary)       return ToXmlDocument((SqlBinary)p);

			// Other Types.
			//

			if (p is Char[])          return ToXmlDocument((Char[])p);
			if (p is Byte[])          return ToXmlDocument((Byte[])p);
			if (p is Binary)          return ToXmlDocument(((Binary)p).ToArray());

			throw CreateInvalidCastException(p.GetType(), typeof(XmlDocument));
		}

#endif

		#endregion

		#endregion

		#region ChangeTypeFromString

		public static object ChangeTypeFromString(string str, Type type)
		{
			if (str == null)
				return null;

			if (type == typeof(string))
				return str;

			var underlyingType = type;
			var isNullable     = false;

			if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				isNullable     = true;
				underlyingType = underlyingType.GetGenericArguments()[0];
			}

			if (underlyingType.IsEnum)
				return Enum.Parse(type, str, false);

			if (isNullable)
			{
				switch (Type.GetTypeCode(underlyingType))
				{
					case TypeCode.Boolean  : return ToNullableBoolean (str);
					case TypeCode.Char     : return ToNullableChar    (str);
					case TypeCode.SByte    : return ToNullableSByte   (str);
					case TypeCode.Byte     : return ToNullableByte    (str);
					case TypeCode.Int16    : return ToNullableInt16   (str);
					case TypeCode.UInt16   : return ToNullableUInt16  (str);
					case TypeCode.Int32    : return ToNullableInt32   (str);
					case TypeCode.UInt32   : return ToNullableUInt32  (str);
					case TypeCode.Int64    : return ToNullableInt64   (str);
					case TypeCode.UInt64   : return ToNullableUInt64  (str);
					case TypeCode.Single   : return ToNullableSingle  (str);
					case TypeCode.Double   : return ToNullableDouble  (str);
					case TypeCode.Decimal  : return ToNullableDecimal (str);
					case TypeCode.DateTime : return ToNullableDateTime(str);
					case TypeCode.Object   :
						if (type == typeof(Guid))           return ToNullableGuid          (str);
						if (type == typeof(DateTimeOffset)) return ToNullableDateTimeOffset(str);
						if (type == typeof(TimeSpan))       return ToNullableTimeSpan      (str);
						break;
					default                : break;
				}
			}
			else
			{
				switch (Type.GetTypeCode(underlyingType))
				{
					case TypeCode.Boolean  : return ToBoolean(str);
					case TypeCode.Char     : return ToChar    (str);
					case TypeCode.SByte    : return ToSByte   (str);
					case TypeCode.Byte     : return ToByte    (str);
					case TypeCode.Int16    : return ToInt16   (str);
					case TypeCode.UInt16   : return ToUInt16  (str);
					case TypeCode.Int32    : return ToInt32   (str);
					case TypeCode.UInt32   : return ToUInt32  (str);
					case TypeCode.Int64    : return ToInt64   (str);
					case TypeCode.UInt64   : return ToUInt64  (str);
					case TypeCode.Single   : return ToSingle  (str);
					case TypeCode.Double   : return ToDouble  (str);
					case TypeCode.Decimal  : return ToDecimal (str);
					case TypeCode.DateTime : return ToDateTime(str);
					default                : break;
				}

				if (type.IsArray)
				{
					if (type == typeof(byte[])) return ToByteArray(str);
				}

				if (type.IsClass)
				{
					if (type == typeof(Binary)) return ToLinqBinary    (str);
				}
			}

			if (type == typeof(Guid))           return ToGuid          (str);
			if (type == typeof(DateTimeOffset)) return ToDateTimeOffset(str);
			if (type == typeof(TimeSpan))       return ToTimeSpan      (str);

#if !SILVERLIGHT

			if (type == typeof(SqlByte))        return ToSqlByte    (str);
			if (type == typeof(SqlInt16))       return ToSqlInt16   (str);
			if (type == typeof(SqlInt32))       return ToSqlInt32   (str);
			if (type == typeof(SqlInt64))       return ToSqlInt64   (str);
			if (type == typeof(SqlSingle))      return ToSqlSingle  (str);
			if (type == typeof(SqlBoolean))     return ToSqlBoolean (str);
			if (type == typeof(SqlDouble))      return ToSqlDouble  (str);
			if (type == typeof(SqlDateTime))    return ToSqlDateTime(str);
			if (type == typeof(SqlDecimal))     return ToSqlDecimal (str);
			if (type == typeof(SqlMoney))       return ToSqlMoney   (str);
			if (type == typeof(SqlString))      return ToSqlString  (str);
			if (type == typeof(SqlGuid))        return ToSqlGuid    (str);

#endif

			return System.Convert.ChangeType(str, type, Thread.CurrentThread.CurrentCulture);
		}


		#endregion

		static Exception CreateInvalidCastException(Type originalType, Type conversionType)
		{
			return new InvalidCastException(string.Format(Resources.Convert_InvalidCast, originalType.FullName, conversionType.FullName));
		}
	}
}
