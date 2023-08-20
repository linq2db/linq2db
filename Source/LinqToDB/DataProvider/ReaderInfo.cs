using System;

namespace LinqToDB.DataProvider
{
	using Extensions;

	// TODO: V4: refactor to readonly struct
	public struct ReaderInfo : IEquatable<ReaderInfo>
	{
		int _hashCode;

		private Type? _toType;
		public  Type?  ToType
		{
			readonly get => _toType;
			set { _toType = value; CalcHashCode(); }
		}

		private Type? _fieldType;
		public  Type?  FieldType
		{
			readonly get => _fieldType;
			set { _fieldType = value; CalcHashCode(); }
		}

		private Type? _providerFieldType;
		public  Type?  ProviderFieldType
		{
			readonly get => _providerFieldType;
			set { _providerFieldType = value; CalcHashCode(); }
		}

		private string? _dataTypeName;
		public  string?  DataTypeName
		{
			readonly get => _dataTypeName;
			set { _dataTypeName = value?.ToLowerInvariant(); CalcHashCode(); }
		}

		private Type? _dataReaderType;
		public Type? DataReaderType
		{
			readonly get => _dataReaderType;
			set { _dataReaderType = value; CalcHashCode(); }
		}


		void CalcHashCode()
		{
			unchecked
			{
				_hashCode = 639348056;
				_hashCode = _hashCode * -1521134295 + (ToType            == null ? 0 : ToType.           GetHashCode  ());
				_hashCode = _hashCode * -1521134295 + (FieldType         == null ? 0 : FieldType.        GetHashCode  ());
				_hashCode = _hashCode * -1521134295 + (ProviderFieldType == null ? 0 : ProviderFieldType.GetHashCode  ());
				_hashCode = _hashCode * -1521134295 + (DataTypeName      == null ? 0 : DataTypeName     .GetHashCodeEx());
				_hashCode = _hashCode * -1521134295 + (DataReaderType    == null ? 0 : DataReaderType   .GetHashCode  ());
			}
		}

		public override bool Equals(object? obj)
		{
			return obj is ReaderInfo ri && Equals(ri);
		}

		public readonly override int GetHashCode()
		{
			return _hashCode;
		}

		public bool Equals(ReaderInfo other)
		{
			return
				ToType            == other.ToType &&
				FieldType         == other.FieldType &&
				ProviderFieldType == other.ProviderFieldType &&
				DataTypeName      == other.DataTypeName &&
				DataReaderType    == other.DataReaderType
				;
		}
	}
}
