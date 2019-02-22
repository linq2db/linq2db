using System;
using System.Text;
using System.Runtime.Serialization;

namespace System.Data.Linq
{
	[DataContract]
	[Serializable]
	public sealed class Binary : IEquatable<Binary>
	{
		[DataMember(Name = "Bytes")]
		byte[] bytes;
		int? hashCode;

		public Binary(byte[] value)
		{
			if(value == null)
			{
				this.bytes = new byte[0];
			}
			else
			{
				this.bytes = new byte[value.Length];
				Array.Copy(value, this.bytes, value.Length);
			}
			this.ComputeHash();
		}

		public byte[] ToArray()
		{
			byte[] copy = new byte[this.bytes.Length];
			Array.Copy(this.bytes, copy, copy.Length);
			return copy;
		}

		public int Length
		{
			get { return this.bytes.Length; }
		}

		public static implicit operator Binary(byte[] value)
		{
			return new Binary(value);
		}

		public bool Equals(Binary other)
		{
			return this.EqualsTo(other);
		}

		public static bool operator ==(Binary binary1, Binary binary2)
		{
			if((object)binary1 == (object)binary2)
				return true;
			if((object)binary1 == null && (object)binary2 == null)
				return true;
			if((object)binary1 == null || (object)binary2 == null)
				return false;
			return binary1.EqualsTo(binary2);
		}

		public static bool operator !=(Binary binary1, Binary binary2)
		{
			if((object)binary1 == (object)binary2)
				return false;
			if((object)binary1 == null && (object)binary2 == null)
				return false;
			if((object)binary1 == null || (object)binary2 == null)
				return true;
			return !binary1.EqualsTo(binary2);
		}

		public override bool Equals(object obj)
		{
			return this.EqualsTo(obj as Binary);
		}

		public override int GetHashCode()
		{
			if(!hashCode.HasValue)
			{
				// hash code is not marked [DataMember], so when
				// using the DataContractSerializer, we'll need
				// to recompute the hash after deserialization.
				ComputeHash();
			}
			return this.hashCode.Value;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("\"");
			sb.Append(System.Convert.ToBase64String(this.bytes, 0, this.bytes.Length));
			sb.Append("\"");
			return sb.ToString();
		}

		private bool EqualsTo(Binary binary)
		{
			if((object)this == (object)binary)
				return true;
			if((object)binary == null)
				return false;
			if(this.bytes.Length != binary.bytes.Length)
				return false;
			if(this.GetHashCode() != binary.GetHashCode())
				return false;
			for(int i = 0, n = this.bytes.Length; i < n; i++)
			{
				if(this.bytes[i] != binary.bytes[i])
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
			hashCode = 0;
			for(int i = 0; i < bytes.Length; i++)
			{
				hashCode = hashCode * s + bytes[i];
				s = s * t;
			}
		}
	}
}

