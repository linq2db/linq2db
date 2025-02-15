using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1441Tests : TestBase
	{

		[Table("Authors")]
		internal sealed class Author
		{
			[PrimaryKey, Column(CanBeNull = false)]
			public int Id { get; set; }

			[Column(CanBeNull = false, DataType = DataType.NVarChar, Length = 100)]
			public string Name { get; set; } = null!;
		}

		[Table("Books")]
		internal sealed class Book
		{
			[PrimaryKey, Column(CanBeNull = false)]
			public int Id { get; set; }

			[Column(CanBeNull = false)]
			public int AuthorId { get; set; }

			[Column(CanBeNull = false, DataType = DataType.NVarChar, Length = 100)]
			public string Title { get; set; } = null!;
		}

		private void SampleSelectTest1([DataSources] string context)
		{
			using (var db           = GetDataContext(context))
			using (var authorsTable = db.CreateLocalTable<Author>())
			using (var booksTable   = db.CreateLocalTable<Book>())
			{
				IQueryable<Author> authors = authorsTable;

				var onlyWithBooks = true; // Some filter logic

				if (onlyWithBooks)
				{
					// referencing authors variable causes StackOverflowException
					// due to circular dependency
					authors =
						from book in booksTable
						from author in authors.InnerJoin(author => author.Id == book.AuthorId)
						select author;
				}

				var authorsList = authors.ToList();

				// Consume authors list
			}
		}

		[Test]
		public void SampleSelectTest2([DataSources] string context)
		{
			using (var db           = GetDataContext(context))
			using (var authorsTable = db.CreateLocalTable<Author>())
			using (var booksTable   = db.CreateLocalTable<Book>())
			{
				IQueryable<Author> authors = authorsTable;

				var onlyWithBooks = true; // Some filter logic

				if (onlyWithBooks)
				{
					authors =
						from author in authors
						from book in booksTable.InnerJoin(book => book.AuthorId == author.Id)
						select author;
				}

				var authorsList = authors.ToList();

				// Consume authors list
			}
		}

	}
}
