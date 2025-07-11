using System.Linq;

using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore.Tests.Models.Inheritance;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Shouldly;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	[TestFixture]
	public class InheritanceTests : ContextTestBase<InheritanceContext>
	{
		protected override InheritanceContext CreateProviderContext(string provider, DbContextOptions<InheritanceContext> options)
		{
			return new InheritanceContext(options);
		}

		[Test]
		public void TestInheritanceBulkCopy([EFDataSources] string provider, [Values] BulkCopyType copyType)
		{
			using var ctx = CreateContext(provider);

			var data = new BlogBase[] { new Blog() { Url = "BlogUrl" }, new RssBlog() { Url = "RssUrl" } };

			ctx.BulkCopy(new BulkCopyOptions() { BulkCopyType = BulkCopyType.RowByRow }, data);

			var items = ctx.Blogs.ToArray();

			items[0].ShouldBeOfType<Blog>();
			((Blog)items[0]).Url.ShouldBe("BlogUrl");

			items[1].ShouldBeOfType<RssBlog>();
			((RssBlog)items[1]).Url.ShouldBe("RssUrl");
		}

		[Test]
		[Ignore("Not supported yet")]
		public void TestInheritanceShadowBulkCopy([EFDataSources] string provider, [Values] BulkCopyType copyType)
		{
			using var ctx = CreateContext(provider);

			var data = new ShadowBlogBase[] { new ShadowBlog() { Url = "BlogUrl" }, new ShadowRssBlog() { Url = "RssUrl" } };

			ctx.BulkCopy(new BulkCopyOptions() { BulkCopyType = BulkCopyType.RowByRow }, data);

			var items = ctx.ShadowBlogs.ToArray();

			items[0].ShouldBeOfType<ShadowBlog>();
			((ShadowBlog)items[0]).Url.ShouldBe("BlogUrl");

			items[1].ShouldBeOfType<ShadowRssBlog>();
			((ShadowRssBlog)items[1]).Url.ShouldBe("RssUrl");
		}
	}
}
