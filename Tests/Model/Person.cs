using System;

using LinqToDB;
using LinqToDB.Mapping;

namespace Tests.Model
{
	public class Person
	{
		public Person()
		{
		}

		public Person(int id)
		{
			ID = id;
		}

		public Person(int id, string firstName)
		{
			ID        = id;
			FirstName = firstName;
		}

		[Identity, PrimaryKey]
		[Column(Name="PersonID", IsIdentity=true, IsPrimaryKey=true)]
		//[SequenceName(ProviderName.PostgreSQL, "Seq")]
		[SequenceName(ProviderName.Firebird,   "PersonID")]
		[MapField("PersonID")]   public int    ID;
		                         public string FirstName { get; set; }
		                         public string LastName;
		[Column(CanBeNull=true)] [Nullable]             public string MiddleName;
		                         public Gender Gender;

		[MapIgnore, NonColumn]   public string Name { get { return FirstName + " " + LastName; }}

		[Association(ThisKey = "ID", OtherKey = "PersonID", CanBeNull = true)]
		public Patient Patient;

		public override bool Equals(object obj)
		{
			return Equals(obj as Person);
		}

		public bool Equals(Person other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return
				other.ID == ID &&
				Equals(other.LastName,   LastName) &&
				Equals(other.MiddleName, MiddleName) &&
				other.Gender == Gender &&
				Equals(other.FirstName,  FirstName);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var result = ID;
				result = (result * 397) ^ (LastName   != null ? LastName.GetHashCode()   : 0);
				result = (result * 397) ^ (MiddleName != null ? MiddleName.GetHashCode() : 0);
				result = (result * 397) ^ Gender.GetHashCode();
				result = (result * 397) ^ (FirstName  != null ? FirstName.GetHashCode()  : 0);
				return result;
			}
		}
	}
}
