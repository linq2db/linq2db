namespace Tests.UserTests
{
	using System.Linq;

	using NUnit.Framework;

	[TestFixture]
	public class Issue569Tests : TestBase
	{
		[Test, DataContextSource]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = from patient in db.Patient
					from person in db.Person
					from doctor in db.Doctor
						.Where(x => x.PersonID == person.ID && x.PersonID == patient.PersonID)
						.DefaultIfEmpty()
					where person.FirstName.StartsWith("J")
					select new
					{
						PersonId  = patient.PersonID,
						FirstName = person.FirstName,
						Taxonomy  = doctor.Taxonomy
					};

				var expected = from patient in Patient
					from person in Person
					from doctor in Doctor
						.Where(x => x.PersonID == person.ID && x.PersonID == patient.PersonID)
						.DefaultIfEmpty()
					where person.FirstName.StartsWith("J")
					select new
					{
						PersonId  = patient.PersonID,
						FirstName = person.FirstName,
						Taxonomy  = doctor != null ? doctor.Taxonomy : null
					};

				AreEqual(expected, result);
			}
		}

		[Test, DataContextSource]
		public void Test2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = 
					from parent     in db.Parent
					from child      in db.Child
					from grandChild in child.GrandChildren.DefaultIfEmpty()
					select new
					{
						parent    .ParentID,
						child     .ChildID,
						grandChild.GrandChildID
					};

				var expected = 
					from parent     in Parent
					from child      in Child
					from grandChild in child.GrandChildren.DefaultIfEmpty()
					select new
					{
						parent     .ParentID,
						child      .ChildID,
						grandChild?.GrandChildID
					};

				AreEqual(expected, result);
			}
		}

		[Test, DataContextSource]
		public void Test3(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p  in Person
							   from pt in Patient
							   from d  in Doctor
							   select new
							   {
								   p. ID,
								   pt.PersonID,
								   d. Taxonomy
							   };

				var result   = from p  in db.Person
							   from pt in db.Patient
							   from d  in db.Doctor
							   select new
							   {
								   p. ID,
								   pt.PersonID,
								   d. Taxonomy
							   };

				AreEqual(expected, result);

			}
		}
	}
}