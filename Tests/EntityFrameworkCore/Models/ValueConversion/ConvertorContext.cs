using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.ValueConversion
{
	public sealed class ConvertorContext : DbContext
	{
		public ConvertorContext(DbContextOptions options) : base(options)
		{
		}

		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public DbSet<SubDivision> Subdivisions { get; set; } = null!;
	}
}
