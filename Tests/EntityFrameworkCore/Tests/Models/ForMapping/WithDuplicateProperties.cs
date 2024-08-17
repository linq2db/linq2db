using System.ComponentModel.DataAnnotations;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping
{
	public class WithDuplicatePropertiesBase
	{
		[Key]
		public int Id { get; set; }

		public virtual string? Value { get; set; }
	}

	public class WithDuplicateProperties : WithDuplicatePropertiesBase
	{
		public new int? Value { get; set; }
	}
}
