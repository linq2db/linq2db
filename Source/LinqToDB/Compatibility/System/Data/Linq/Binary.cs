#if !NETFRAMEWORK
#pragma warning disable IDE0130

using System.Runtime.Serialization;

namespace System.Data.Linq
{
	using LinqToDB.Common;

	[DataContract]
	[Serializable]
	public sealed class Binary : IEquatable<Binary>
	{
		[DataMember(Name = "Bytes")]
		private byte[] _bytes;
		private int?   _hashCode;

		public Binary(byte[]? value)
		{
			if(value == null)
			{
				_bytes = [];
			}
			else
			{
				_bytes = new byte[value.Length];
				Array.Copy(value, _bytes, value.Length);
			}
		}

		public byte[] ToArray()
		{
			var copy = new byte[_bytes.Length];
			Array.Copy(_bytes, copy, copy.Length);
			return copy;
		}

		public int Length => _bytes.Length;

		public static implicit operator Binary(byte[]? value) => new Binary(value);

		public bool Equals(Binary? other) => EqualsTo(other);

		public static bool operator ==(Binary? binary1, Binary? binary2)
		{
			if (ReferenceEquals(binary1, binary2))
				return true;
			if (binary1 is null || binary2 is null)
				return false;

			return binary1.Equals(binary2);
		}

		public static bool operator !=(Binary? binary1, Binary? binary2) => !(binary1 == binary2);

		public override bool Equals(object? obj) => EqualsTo(obj as Binary);

		public override int GetHashCode()
		{
			if(!_hashCode.HasValue)
			{
				// hash code is not marked [DataMember], so when
				// using the DataContractSerializer, we'll need
				// to recompute the hash after deserialization.
				ComputeHash();
			}
			return _hashCode!.Value;
		}

		public override string ToString() => $"\"{Convert.ToBase64String(_bytes, 0, _bytes.Length)}\"";

		private bool EqualsTo(Binary? binary)
		{
			if (binary == null)
				return false;
			if(ReferenceEquals(this, binary))
				return true;
			if(_bytes.Length != binary._bytes.Length)
				return false;
			if(GetHashCode() != binary.GetHashCode())
				return false;
			for(int i = 0, n = _bytes.Length; i < n; i++)
			{
				if(_bytes[i] != binary._bytes[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Simple hash using pseudo-random coefficients for each byte in
		/// the array to achieve order dependency.
		/// </summary>
		private void ComputeHash()
		{
			int s = 314, t = 159;
			_hashCode = 0;
			for(var i = 0; i < _bytes.Length; i++)
			{
				_hashCode = _hashCode * s + _bytes[i];
				s *= t;
			}
		}
	}
}
#endif
