using System;
using System.Runtime.Serialization;
using System.Text;

namespace System
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
	class SerializableAttribute : Attribute
	{
	}

	namespace Collections
	{
		namespace Concurrent
		{
			internal class ConcurrentDictionary<TKey,TValue> : Generic.Dictionary<TKey,TValue>
			{
				public TValue GetOrAdd(TKey key, Func<TKey,TValue> valueFactory)
				{
					if ((object)key == null)
						throw new ArgumentNullException("key");

					if (valueFactory == null)
						throw new ArgumentNullException("valueFactory");

					TValue resultingValue;

					if (TryGetValue(key, out resultingValue))
						return resultingValue;

					this[key] = resultingValue = valueFactory(key);

					return resultingValue;
				}
			}
		}
	}

	namespace Data
	{
		public interface IDataRecord
		{
			int         FieldCount        { get; }
			object      this[int i]       { get; }
			object      this[string name] { get; }

			string      GetName        (int i);
			string      GetDataTypeName(int i);
			Type        GetFieldType   (int i);
			object      GetValue       (int i);
			int         GetValues      (object[] values);
			int         GetOrdinal     (string name);
			bool        GetBoolean     (int i);
			byte        GetByte        (int i);
			long        GetBytes       (int i, long fieldOffset, byte[] buffer, int bufferoffset, int length);
			char        GetChar        (int i);
			long        GetChars       (int i, long fieldoffset, char[] buffer, int bufferoffset, int length);
			Guid        GetGuid        (int i);
			short       GetInt16       (int i);
			int         GetInt32       (int i);
			long        GetInt64       (int i);
			float       GetFloat       (int i);
			double      GetDouble      (int i);
			string      GetString      (int i);
			decimal     GetDecimal     (int i);
			DateTime    GetDateTime    (int i);
			IDataReader GetData        (int i);
			bool        IsDBNull       (int i);
		}

		public interface IDataReader : IDisposable, IDataRecord
		{
			int       Depth { get; }
			bool      IsClosed { get; }
			int       RecordsAffected { get; }
			void      Close();
			//DataTable GetSchemaTable();
			bool      NextResult();
			bool      Read();
		}

		public enum SqlDbType
		{
			BigInt           = 0,
			Binary           = 1,
			Bit              = 2,
			Char             = 3,
			DateTime         = 4,
			Decimal          = 5,
			Float            = 6,
			Image            = 7,
			Int              = 8,
			Money            = 9,
			NChar            = 10,
			NText            = 11,
			NVarChar         = 12,
			Real             = 13,
			UniqueIdentifier = 14,
			SmallDateTime    = 15,
			SmallInt         = 16,
			SmallMoney       = 17,
			Text             = 18,
			Timestamp        = 19,
			TinyInt          = 20,
			VarBinary        = 21,
			VarChar          = 22,
			Variant          = 23,
			Xml              = 25,
			Udt              = 29,
			Structured       = 30,
			Date             = 31,
			Time             = 32,
			DateTime2        = 33,
			DateTimeOffset   = 34,
		}

		public enum DbType
		{
			AnsiString            = 0,
			Binary                = 1,
			Byte                  = 2,
			Boolean               = 3,
			Currency              = 4,
			Date                  = 5,
			DateTime              = 6,
			Decimal               = 7,
			Double                = 8,
			Guid                  = 9,
			Int16                 = 10,
			Int32                 = 11,
			Int64                 = 12,
			Object                = 13,
			SByte                 = 14,
			Single                = 15,
			String                = 16,
			Time                  = 17,
			UInt16                = 18,
			UInt32                = 19,
			UInt64                = 20,
			VarNumeric            = 21,
			AnsiStringFixedLength = 22,
			StringFixedLength     = 23,
			Xml                   = 25,
			DateTime2             = 26,
			DateTimeOffset        = 27,
		}

		namespace Linq
		{
			[DataContract]
			[Serializable]
			public sealed class Binary : IEquatable<Binary>
			{
				[DataMember(Name="Bytes")] byte[] _bytes;

				int? _hashCode;

				public Binary(byte[] value)
				{
					if (value == null) 
						throw new ArgumentNullException("value");

					_bytes = new byte[value.Length];
					Array.Copy(value, _bytes, value.Length);
					ComputeHash();
				} 

				public byte[] ToArray()
				{
					var copy = new byte[this._bytes.Length];
					Array.Copy(_bytes, copy, copy.Length);
					return copy;
				} 
 
				public int Length
				{
					get { return _bytes.Length; }
				}

				public static implicit operator Binary(byte[] value)
				{
					return new Binary(value);
				}
 
				public bool Equals(Binary other)
				{
					return EqualsTo(other);
				}

				public static bool operator == (Binary binary1, Binary binary2)
				{
					if ((object)binary1 == (object)binary2)                 return true;
					if ((object)binary1 == null && (object)binary2 == null) return true;
					if ((object)binary1 == null || (object)binary2 == null) return false;

					return binary1.EqualsTo(binary2);
				}

				public static bool operator !=(Binary binary1, Binary binary2)
				{
					if ((object)binary1 == (object)binary2)                 return false;
					if ((object)binary1 == null && (object)binary2 == null) return false;
					if ((object)binary1 == null || (object)binary2 == null) return true;

					return !binary1.EqualsTo(binary2);
				}

				public override bool Equals(object obj)
				{
					return EqualsTo(obj as Binary);
				}
 
				public override int GetHashCode()
				{
					if (!_hashCode.HasValue)
						ComputeHash();

					return _hashCode.Value;
				}

				public override string ToString()
				{
					var sb = new StringBuilder();

					sb.Append("\"");
					sb.Append(Convert.ToBase64String(_bytes, 0, _bytes.Length));
					sb.Append("\""); 

					return sb.ToString();
				} 
 
				private bool EqualsTo(Binary binary)
				{
					if ((object)this   == (object)binary)       return true;
					if ((object)binary == null)                 return false;
					if (_bytes.Length  != binary._bytes.Length) return false;
					if (_hashCode      != binary._hashCode)     return false;

					for (int i = 0; i < _bytes.Length; i++)
						if (_bytes[i] != binary._bytes[i])
							return false;

					return true;
				} 

				private void ComputeHash()
				{
					var s = 314;
					const int t = 159;

					_hashCode = 0;

					for (var i = 0; i < _bytes.Length; i++)
					{
						_hashCode = _hashCode * s + _bytes[i];
						s = s * t;
					}
				}
			}
		}

		public enum ParameterDirection
		{
			Input       = 1,
			Output      = 2,
			InputOutput = 3,
			ReturnValue = 6,
		}
	}
}
