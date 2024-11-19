namespace LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping
{
	public class WithInheritance
	{
		public int Id { get; set; }
		public string Discriminator { get; set; } = null!;
	}

	public class WithInheritanceA : WithInheritance
	{

	}

	public class WithInheritanceA1 : WithInheritanceA
	{

	}

	public class WithInheritanceA2 : WithInheritanceA
	{

	}
}
