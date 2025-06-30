using System;

using LinqToDB.Mapping;

namespace Tests.Model
{
	public class Patient
	{
		[PrimaryKey]
		public int    PersonID;
		public string Diagnosis = null!;

		[Association(ThisKey = "PersonID", OtherKey = "ID", CanBeNull = false)]
		public Person Person = null!;

		public override bool Equals(object? obj)
		{
			return Equals(obj as Patient);
		}

		public bool Equals(Patient? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.PersonID == PersonID && Equals(other.Diagnosis,  Diagnosis);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(PersonID, Diagnosis);
		}
	}
}
