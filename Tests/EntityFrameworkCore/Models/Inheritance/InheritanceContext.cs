using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Inheritance
{
	public abstract class BlogBase
	{
		public int Id { get; set; }
		public string BlogType { get; set; } = null!;
	}

	public class Blog : BlogBase
	{
		public string Url { get; set; } = null!;
	}

	public class RssBlog : BlogBase
	{
		public string Url { get; set; } = null!;
	}

	public abstract class ShadowBlogBase
	{
		public int Id { get; set; }
		public string BlogType { get; set; } = null!;
	}

	public class ShadowBlog : ShadowBlogBase
	{
		public string Url { get; set; } = null!;
	}

	public class ShadowRssBlog : ShadowBlogBase
	{
		public string Url { get; set; } = null!;
	}

	public class InheritanceContext : DbContext
	{
		public InheritanceContext(DbContextOptions options) : base(options)
		{
			
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<BlogBase>()
				.HasDiscriminator(b => b.BlogType)
				.HasValue<Blog>("blog_base")
				.HasValue<RssBlog>("blog_rss");

			modelBuilder.Entity<BlogBase>()
				.Property(e => e.BlogType)
				.HasColumnName("BlogType")
				.HasMaxLength(200);

			modelBuilder.Entity<Blog>()
				.Property(b => b.Url)
				.HasColumnName("Url");

			modelBuilder.Entity<RssBlog>()
				.Property(b => b.Url)
				.HasColumnName("Url");

#if !NETFRAMEWORK
			modelBuilder.Entity<Blog>().ToTable("Blogs");
			modelBuilder.Entity<RssBlog>().ToTable("Blogs");
#else
			modelBuilder.Entity<BlogBase>().ToTable("Blogs");
#endif

			/////

			modelBuilder.Entity<ShadowBlogBase>()
				.HasDiscriminator()
				.HasValue<ShadowBlog>("blog_base")
				.HasValue<ShadowRssBlog>("blog_rss");

			modelBuilder.Entity<ShadowBlogBase>()
				.Property(e => e.BlogType)
				.HasColumnName("BlogType")
				.HasMaxLength(200);

			modelBuilder.Entity<ShadowBlog>()
				.Property(b => b.Url)
				.HasColumnName("Url");

			modelBuilder.Entity<ShadowRssBlog>()
				.Property(b => b.Url)
				.HasColumnName("Url");

#if !NETFRAMEWORK
			modelBuilder.Entity<ShadowBlog>().ToTable("ShadowBlogs");
			modelBuilder.Entity<ShadowRssBlog>().ToTable("ShadowBlogs");
#else
			modelBuilder.Entity<ShadowBlogBase>().ToTable("ShadowBlogs");
#endif
		}

		public DbSet<BlogBase> Blogs { get; set; } = null!;
		public DbSet<ShadowBlogBase> ShadowBlogs { get; set; } = null!;
	}

}
