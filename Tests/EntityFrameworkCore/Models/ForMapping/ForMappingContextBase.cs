using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping
{
	public abstract class ForMappingContextBase : DbContext
	{
		protected ForMappingContextBase(DbContextOptions options) : base(options)
		{
		}

		public DbSet<WithIdentity> WithIdentity { get; set; } = null!;
		public DbSet<NoIdentity> NoIdentity { get; set; } = null!;
		public DbSet<UIntTable> UIntTable { get; set; } = null!;
		public DbSet<StringTypes> StringTypes { get; set; } = null!;
		public DbSet<TypesTable> Types { get; set; } = null!;

		public DbSet<WithDuplicateProperties> WithDuplicateProperties { get; set; } = null!;
		
		public DbSet<WithInheritance> WithInheritance { get; set; } = null!;
		public DbSet<WithInheritanceA> WithInheritanceA { get; set; } = null!;
		public DbSet<WithInheritanceA1> WithInheritanceA1 { get; set; } = null!;
		public DbSet<WithInheritanceA2> WithInheritanceA2 { get; set; } = null!;
	}
}
