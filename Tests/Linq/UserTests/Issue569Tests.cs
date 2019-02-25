namespace Tests.UserTests
{
	using System.Linq;
	using LinqToDB;
	using NUnit.Framework;

	[TestFixture]
	public class Issue569Tests : TestBase
	{
		[Test]
		public void Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = from patient in db.Patient
					from person in db.Person
					from doctor in db.Doctor
						.Where(x => x.PersonID == person.ID && x.PersonID == patient.PersonID)
						.DefaultIfEmpty()
					where person.FirstName.StartsWith("J")
					orderby patient.PersonID, person.FirstName, doctor.Taxonomy
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
					orderby patient.PersonID, person.FirstName, doctor != null ? doctor.Taxonomy : null
					select new
					{
						PersonId  = patient.PersonID,
						FirstName = person.FirstName,
						Taxonomy  = doctor != null ? doctor.Taxonomy : null
					};

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Test2([DataSources] string context)
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

		[ActiveIssue(Configurations = new[] { ProviderName.SapHana })]
		[Test]
		public void Test3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p  in Person
							   from pt in Patient
							   from d  in Doctor
							   orderby p.ID, pt.PersonID, d.Taxonomy
							   select new
							   {
								   p. ID,
								   pt.PersonID,
								   d. Taxonomy
							   };

				var result   = from p  in db.Person
							   from pt in db.Patient
							   from d  in db.Doctor
							   orderby p.ID, pt.PersonID, d.Taxonomy
							   select new
							   {
								   p. ID,
								   pt.PersonID,
								   d. Taxonomy
							   };

				AreEqual(expected, result);

			}
		}

		[Test]
		public void Test4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var sq =
					from parent     in db.Parent
					from child      in db.Child
					from grandChild in child.GrandChildren.DefaultIfEmpty()
					select new
					{
						parent    .ParentID,
						child     .ChildID,
						grandChild.GrandChildID
					};

				var q =
					from parent in db.Parent
					from s in sq
					select new
					{
						parent,
						s
					};

				var rsq =
					from parent     in Parent
					from child      in Child
					from grandChild in child.GrandChildren.DefaultIfEmpty()
					select new
					{
						parent    .ParentID,
						child     .ChildID,
						GrandChildID = grandChild != null ? grandChild.GrandChildID : null
					};

				var rq =
					from parent in Parent
					from s in rsq
					select new
					{
						parent,
						s
					};

				Assert.AreEqual(rq.Count(), q.Count());
			}
		}

		[Test]
		public void Test5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var sq =
					from parent     in db.Parent
					from child      in db.Child
					from grandChild in child.GrandChildren.DefaultIfEmpty()
					select new
					{
						parent    .ParentID,
						child     .ChildID,
						grandChild.GrandChildID
					};

				var q =
					from s in sq
					from parent in db.Parent
					select new
					{
						parent,
						s
					};

				var rsq =
					from parent     in Parent
					from child      in Child
					from grandChild in child.GrandChildren.DefaultIfEmpty()
					select new
					{
						parent    .ParentID,
						child     .ChildID,
						GrandChildID = grandChild != null ? grandChild.GrandChildID : null
					};

				var rq =
					from s in rsq
					from parent in Parent
					select new
					{
						parent,
						s
					};

				Assert.AreEqual(rq.Count(), q.Count());
			}
		}
	}
}
