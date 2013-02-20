using System;

using LinqToDB;
using LinqToDB.Mapping;

namespace Tests.Model
{
	public class Patient
	{
		[PrimaryKey]
		[Column(IsPrimaryKey=true)]
		public int    PersonID;
		public string Diagnosis;

		[Association(ThisKey = "PersonID", OtherKey = "ID", CanBeNull = false)]
		public Person Person;

		public override bool Equals(object obj)
		{
			return Equals(obj as Patient);
		}

		public bool Equals(Patient other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.PersonID == PersonID && Equals(other.Diagnosis,  Diagnosis);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var result = PersonID;
				result = (result * 397) ^ (Diagnosis != null ? Diagnosis.GetHashCode() : 0);
				return result;
			}
		}
	}
}
