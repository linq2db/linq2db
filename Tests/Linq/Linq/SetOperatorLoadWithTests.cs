using System.Linq.Expressions;
using System.Net;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class SetOperatorLoadWithTests : TestBase
	{
		class Author
		{
			[PrimaryKey]
			public int Id { get;             set; }

			public string AuthorName { get; set; } = default!;

			[Association(QueryExpressionMethod = nameof(BooksImpl))]
			public List<Book> Books { get; set; } = default!;

			static Expression<Func<Author, IDataContext, IQueryable<Book>>> BooksImpl()
			{
				return (author, db) =>
					db.GetTable<BookAuthor>().Where(ba => ba.AuthorId == author.Id).Select(ba => ba.Book);
			}

			[Association(QueryExpressionMethod = nameof(CoAuthorsImpl))]
			public List<Book> CoAuthors { get; set; } = default!;

			static Expression<Func<Author, IEnumerable<Author>>> CoAuthorsImpl()
			{
				return author =>
					from b in author.Books
					from a in b.Authors
					where a.Id != author.Id
					select a;
			}

		}

		[InheritanceMapping(Code = "Roman", Type = typeof(Roman))]
		[InheritanceMapping(Code = "Novel", Type = typeof(Novel))]
		abstract class Book
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public abstract string Discriminator { get; }

			public string BookName { get; set; } = default!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Chapter.BookId))]
			public List<Chapter> Chapters { get; set; } = default!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Article.BookId))]
			public List<Article> Articles { get; set; } = default!;

			[Association(QueryExpressionMethod = nameof(AuthorsImpl))]
			public List<Author> Authors { get; set; } = default!;

			static Expression<Func<Book, IDataContext, IQueryable<Author>>> AuthorsImpl()
			{
				return (author, db) =>
					db.GetTable<BookAuthor>().Where(ba => ba.AuthorId == author.Id).Select(ba => ba.Author);
			}
		}

		class BookAuthor
		{
			public int BookId   { get; set; }
			public int AuthorId { get; set; }

			[Association(ThisKey = nameof(BookId), OtherKey = "Id")]
			public Book   Book   { get; set; } = default!;

			[Association(ThisKey = nameof(AuthorId), OtherKey = "Id")]
			public Author Author { get; set; } = default!;
		}

		class Roman : Book
		{
			public override string Discriminator => "Roman";
		}

		class Novel : Book
		{
			public override string Discriminator => "Novel";
		}

		class Chapter
		{
			[PrimaryKey]
			public int Id { get; set; }

			public int BookId { get; set; }

			public string ChapterName { get; set; } = default!;

			[Association(ThisKey = nameof(BookId), OtherKey = nameof(Id))]
			public Book Book { get; set; } = default!;
		}

		class Article
		{
			[PrimaryKey]
			public int Id { get; set; }

			public int    BookId      { get; set; }
			public int    AuthorId    { get; set; }
			public string ArticleName { get; set; } = default!;

			[Association(ThisKey = nameof(BookId), OtherKey = "Id")]
			public Book Book { get; set; } = default!;

			[Association(ThisKey = nameof(AuthorId), OtherKey = "Id")]
			public Author Author { get; set; } = default!;
		}

		class TablesDisposal : IDisposable
		{
			readonly IDisposable[] _toDispose;

			public TablesDisposal(params IDisposable[] toDispose)
			{
				_toDispose = toDispose;
			}

			public void Dispose()
			{
				foreach(var d in _toDispose)
					d.Dispose();
			}
		}

		IDisposable InitTestData(IDataContext db)
		{
			var authors = new[]
			{
				new Author { Id = 1, AuthorName = "Stephen King" }, 
				new Author { Id = 2, AuthorName = "Harry Harrison" }, 
				new Author { Id = 3, AuthorName = "Roger Joseph Zelazny" }, 
			};

			var books = new Book[]
			{
				new Roman {Id = 11, BookName = "Lisey's Story["},
				new Novel {Id = 12, BookName = "Duma Key"},
				new Roman {Id = 13, BookName = "Just After Sunset"},
						  
				new Roman {Id = 21, BookName = "Deathworld"},
				new Novel {Id = 22, BookName = "The Stainless Steel Rat"},
				new Roman {Id = 23, BookName = "Planet of the Damned"},
						  
				new Roman {Id = 31, BookName = "Blood of Amber"},
				new Novel {Id = 32, BookName = "Knight of Shadows"},
				new Roman {Id = 33, BookName = "The Chronicles of Amber"}
			};

			var bookAuthor = new BookAuthor[]
			{
				new (){AuthorId = 1, BookId = 11},
				new (){AuthorId = 1, BookId = 12},
				new (){AuthorId = 1, BookId = 13},

				new (){AuthorId = 2, BookId = 21},
				new (){AuthorId = 2, BookId = 22},
				new (){AuthorId = 2, BookId = 23},

				new (){AuthorId = 3, BookId = 31},
				new (){AuthorId = 3, BookId = 32},
				new (){AuthorId = 3, BookId = 33},
			};

			return new TablesDisposal(
				db.CreateLocalTable(authors),
				db.CreateLocalTable(books),
				db.CreateLocalTable(bookAuthor));
		}

		[Test]
		public void ConcatEqualLoadWith([DataSources] string context)
		{
			using var db       = GetDataContext(context);
			using var disposal = InitTestData(db);

			var authorTable = db.GetTable<Author>();
			var bookTable = db.GetTable<Book>();

			var query1 = 
				from a in authorTable.LoadWith(a => a.Books)
				from b in a.Books.OfType<Roman>()
				select (Book)b;

			var query2 = 
				from a in authorTable.LoadWith(a => a.Books)
				from b in a.Books.OfType<Novel>()
				select b;

			var query = query1.Concat(query2);

			var result = query.ToArray();

			AssertQuery(query);
		}
		
	}
}
