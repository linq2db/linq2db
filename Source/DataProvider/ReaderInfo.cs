using System;

namespace LinqToDB.DataProvider
{
	public class ReaderInfo : IEquatable<ReaderInfo>
	{
		int _hashCode;

		private Type _toType;
		public  Type  ToType
		{
			get { return _toType; }
			set { _toType = value; CalcHashCode(); }
		}

		private Type _fieldType;
		public  Type  FieldType
		{
			get { return _fieldType; }
			set { _fieldType = value; CalcHashCode(); }
		}

		private Type _providerFieldType;
		public  Type  ProviderFieldType
		{
			get { return _providerFieldType; }
			set { _providerFieldType = value; CalcHashCode(); }
		}

		private string _dataTypeName;
		public  string  DataTypeName
		{
			get { return _dataTypeName; }
			set { _dataTypeName = value; CalcHashCode(); }
		}

		void CalcHashCode()
		{
			unchecked
			{
				_hashCode = 639348056;
				_hashCode = _hashCode * -1521134295 + (ToType            == null ? 0 : ToType.           GetHashCode());
				_hashCode = _hashCode * -1521134295 + (FieldType         == null ? 0 : FieldType.        GetHashCode());
				_hashCode = _hashCode * -1521134295 + (ProviderFieldType == null ? 0 : ProviderFieldType.GetHashCode());
				_hashCode = _hashCode * -1521134295 + (DataTypeName      == null ? 0 : DataTypeName.     GetHashCode());
			}
		}

		public override bool Equals(object obj)
		{
			return Equals((ReaderInfo)obj);
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}

		public bool Equals(ReaderInfo other)
		{
			return
				ToType            == other.ToType &&
				FieldType         == other.FieldType &&
				ProviderFieldType == other.ProviderFieldType &&
				DataTypeName      == other.DataTypeName
				;
		}
	}
}
