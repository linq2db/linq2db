using System;
using System.Linq;
using LinqToDB;
using NUnit.Framework;

namespace Tests.UserTests
{
	[LinqToDB.Mapping.TableAttribute("Substitutions", IsColumnAttributeRequired = false)]
	public class Substitution
	{
		[LinqToDB.Mapping.PrimaryKeyAttribute()]
		public Guid Id { get; set; }

		public Guid UserId { get; set; }

		[LinqToDB.Mapping.AssociationAttribute(ThisKey = "UserId", OtherKey = "Id")]
		public User User { get; set; }

		public DateTime StartDate { get; set; }

		public DateTime EndDate { get; set; }
	}

	[LinqToDB.Mapping.TableAttribute("Users", IsColumnAttributeRequired = false)]
	public class User
	{
		public Guid Id { get; set; }
	}

	public class SubstitutionUsers
	{
		public Guid UserId { get; set; }
		public Guid SubstitutionId { get; set; }
	}

	[TestFixture]
	public class Issue708Tests : TestBase
	{
		[Test, DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				User user = new User();
				user.Id = Guid.Empty;


				var subQuery = from substitutionUsers in db.GetTable<SubstitutionUsers>()
					where substitutionUsers.UserId == user.Id
					select substitutionUsers.SubstitutionId;

				var query = from substitution in db.GetTable<Substitution>()
							where subQuery.Contains(substitution.Id) &&
					      substitution.StartDate <= DateTime.Now &&
					      substitution.EndDate >= DateTime.Now
					select substitution.User;

				Assert.DoesNotThrow(() => Console.WriteLine(query.ToString()));
			}
		}
	}
}