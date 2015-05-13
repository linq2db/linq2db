using System;

using LinqToDB;
using LinqToDB.Mapping;

namespace Tests.Model
{
	public class FirsLastName
	{
		public string FirstName { get; set; }
		public string LastName;
	}

	public class FullName
	{
		public FirsLastName Name;
		[Nullable]
		public string MiddleName;
	}

	[Table("Person", IsColumnAttributeRequired = false)]
	[Column("FirstName",  "Name.Name.FirstName")]
	[Column("MiddleName", "Name.MiddleName")]
	[Column("LastName",   "Name.Name.LastName")]
	public class ComplexPerson : IPerson
	{

		[Identity]
		[SequenceName(ProviderName.Firebird, "PersonID")]
		[Column("PersonID", IsPrimaryKey = true)]
		public int      ID     { get; set; }
		public Gender   Gender { get; set; }
		public FullName Name = new FullName();

		[NotColumn]
		int IPerson.ID
		{
			get { return ID;  }
			set { ID = value; }
		}

		[NotColumn]
		Gender IPerson.Gender
		{
			get { return Gender;  }
			set { Gender = value; }
		}

		[NotColumn]
		string IPerson.FirstName
		{
			get { return Name.Name.FirstName;  }
			set { Name.Name.FirstName = value; }
		}

		[NotColumn]
		string IPerson.MiddleName
		{
			get { return Name.MiddleName;  }
			set { Name.MiddleName = value; }
		}

		[NotColumn]
		string IPerson.LastName
		{
			get { return Name.Name.LastName;  }
			set { Name.Name.LastName = value; }
		}
	}
}
