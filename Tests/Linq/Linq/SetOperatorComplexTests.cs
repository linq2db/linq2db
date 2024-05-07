using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class SetOperatorComplexTests : TestBase
	{
		class Author
		{
			[PrimaryKey]
			public int AuthorId { get;             set; }

			public string AuthorName { get; set; } = default!;

			[Association(QueryExpressionMethod = nameof(BooksImpl))]
			public List<Book> Books { get; set; } = default!;

			static Expression<Func<Author, IDataContext, IQueryable<Book>>> BooksImpl()
			{
				return (author, db) =>
					db.GetTable<BookAuthor>().Where(ba => ba.FkAuthorId == author.AuthorId).Select(ba => ba.Book);
			}

			[Association(QueryExpressionMethod = nameof(CoAuthorsImpl))]
			public List<Book> CoAuthors { get; set; } = default!;

			static Expression<Func<Author, IEnumerable<Author>>> CoAuthorsImpl()
			{
				return author =>
					from b in author.Books
					from a in b.Authors
					where a.AuthorId != author.AuthorId
					select a;
			}

		}

		[InheritanceMapping(Code = "Roman", Type = typeof(Roman))]
		[InheritanceMapping(Code = "Novel", Type = typeof(Novel))]
		abstract class Book
		{
			[PrimaryKey]
			public int BookId { get; set; }

			[Column(IsDiscriminator = true)]
			public abstract string Discriminator { get; }

			public string BookName { get; set; } = default!;

			[Association(ThisKey = nameof(BookId), OtherKey = nameof(Chapter.FkBookId))]
			public List<Chapter> Chapters { get; set; } = default!;

			[Association(ThisKey = nameof(BookId), OtherKey = nameof(Article.FkBookId))]
			public List<Article> Articles { get; set; } = default!;

			[Association(QueryExpressionMethod = nameof(AuthorsImpl))]
			public List<Author> Authors { get; set; } = default!;

			static Expression<Func<Book, IDataContext, IQueryable<Author>>> AuthorsImpl()
			{
				return (book, db) =>
					db.GetTable<BookAuthor>().Where(ba => ba.FkBookId == book.BookId).Select(ba => ba.Author);
			}
		}

		class BookAuthor
		{
			public int FkBookId   { get; set; }
			public int FkAuthorId { get; set; }

			[Association(ThisKey = nameof(FkBookId), OtherKey = "BookId")]
			public Book   Book   { get; set; } = default!;

			[Association(ThisKey = nameof(FkAuthorId), OtherKey = "AuthorId")]
			public Author Author { get; set; } = default!;
		}

		class Roman : Book
		{
			public override string Discriminator => "Roman";

			[Column(CanBeNull = true)]
			public int RomanScore { get; set; }
		}

		class Novel : Book
		{
			public override string Discriminator => "Novel";

			[Column(CanBeNull = true)]
			public int NovelScore { get; set; }
		}

		class Chapter
		{
			[PrimaryKey]
			public int ChaperId { get; set; }

			public int FkBookId { get; set; }

			public string ChapterName { get; set; } = default!;

			[Association(ThisKey = nameof(FkBookId), OtherKey = nameof(ChaperId))]
			public Book Book { get; set; } = default!;
		}

		class Article
		{
			[PrimaryKey]
			public int ArticleId { get; set; }

			public int    FkBookId    { get; set; }
			public int    FkAuthorId  { get; set; }
			public string ArticleName { get; set; } = default!;

			[Association(ThisKey = nameof(FkBookId), OtherKey = "BookId")]
			public Book Book { get; set; } = default!;

			[Association(ThisKey = nameof(FkAuthorId), OtherKey = "AuthorId")]
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
				new Author { AuthorId = 1, AuthorName = "Stephen King" }, 
				new Author { AuthorId = 2, AuthorName = "Harry Harrison" }, 
				new Author { AuthorId = 3, AuthorName = "Roger Joseph Zelazny" }, 
			};

			var books = new Book[]
			{
				new Roman {BookId = 11, BookName = "Lisey's Story[", RomanScore = 4},
				new Novel {BookId = 12, BookName = "Duma Key"},
				new Roman {BookId = 13, BookName = "Just After Sunset", RomanScore = 3},
						  
				new Roman {BookId = 21, BookName = "Deathworld", RomanScore = 1},
				new Novel {BookId = 22, BookName = "The Stainless Steel Rat"},
				new Roman {BookId = 23, BookName = "Planet of the Damned"},
						  
				new Roman {BookId = 31, BookName = "Blood of Amber", RomanScore = 5},
				new Novel {BookId = 32, BookName = "Knight of Shadows"},
				new Roman {BookId = 33, BookName = "The Chronicles of Amber", RomanScore = 7}
			};

			var bookAuthor = new BookAuthor[]
			{
				new (){FkAuthorId = 1, FkBookId = 11},
				new (){FkAuthorId = 1, FkBookId = 12},
				new (){FkAuthorId = 1, FkBookId = 13},

				new (){FkAuthorId = 2, FkBookId = 21},
				new (){FkAuthorId = 2, FkBookId = 22},
				new (){FkAuthorId = 2, FkBookId = 23},

				new (){FkAuthorId = 3, FkBookId = 31},
				new (){FkAuthorId = 3, FkBookId = 32},
				new (){FkAuthorId = 3, FkBookId = 33},
			};

			return new TablesDisposal(
				db.CreateLocalTable(authors),
				db.CreateLocalTable(books),
				db.CreateLocalTable(bookAuthor));
		}

		[Test]
		public void ConcatInheritance([DataSources] string context)
		{
			using var db       = GetDataContext(context);
			using var disposal = InitTestData(db);

			var authorTable = db.GetTable<Author>();

			var query1 = 
				from a in authorTable.LoadWith(a => a.Books)
				from b in a.Books.OfType<Roman>()
				select (Book)b;

			var query2 = 
				from a in authorTable.LoadWith(a => a.Books)
				from b in a.Books.OfType<Novel>()
				select b;

			var query = query1.Concat(query2);

			AssertQuery(query);
		}

		[Test]
		public void UnionInheritance([DataSources] string context)
		{
			using var db       = GetDataContext(context);
			using var disposal = InitTestData(db);

			var authorTable = db.GetTable<Author>();

			var query1 = 
				from a in authorTable.LoadWith(a => a.Books)
				from b in a.Books.OfType<Roman>()
				select (Book)b;

			var query2 = 
				from a in authorTable.LoadWith(a => a.Books)
				from b in a.Books.OfType<Novel>()
				select b;

			var query = query1.Union(query2);

			AssertQuery(query);
		}

		[Test]
		public void ExceptInheritance([DataSources] string context)
		{
			using var db       = GetDataContext(context);
			using var disposal = InitTestData(db);

			var authorTable = db.GetTable<Author>();

			var query1 = 
				from a in authorTable.LoadWith(a => a.Books)
				from b in a.Books.OfType<Roman>()
				select (Book)b;

			var query2 = 
				from a in authorTable.LoadWith(a => a.Books)
				from b in a.Books.OfType<Novel>()
				select b;

			var query = query1.Except(query2);

			AssertQuery(query);
		}

		[Test]
		public void IntersectInheritance([DataSources] string context)
		{
			using var db       = GetDataContext(context);
			using var disposal = InitTestData(db);

			var authorTable = db.GetTable<Author>();

			var query1 = 
				from a in authorTable.LoadWith(a => a.Books)
				from b in a.Books.OfType<Roman>()
				select new
				{
					Id = b.BookId,
					b.BookName
				};

			var query2 = 
				from a in authorTable.LoadWith(a => a.Books)
				from b in a.Books.OfType<Novel>()
				select new
				{
					Id = b.BookId,
					b.BookName
				};

			var query = query1.Intersect(query2);

			AssertQuery(query);
		}

		static IQueryable<T> Combine<T>(IQueryable<T> query1, IQueryable<T> query2, SetOperation operation)
		{
			switch (operation)
			{
				case SetOperation.Union:
					return query1.Union(query2);
				case SetOperation.UnionAll:
					return query1.UnionAll(query2);
				case SetOperation.Except:
					return query1.Except(query2);
				case SetOperation.ExceptAll:
					return query1.ExceptAll(query2);
				case SetOperation.Intersect:
					return query1.Intersect(query2);
				case SetOperation.IntersectAll:
					return query1.IntersectAll(query2);
				default:
					throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
			}
		}

		[Test]
		public void EagerSameDetails([DataSources] string context, [Values] SetOperation operation)
		{
			using var db       = GetDataContext(context);
			using var disposal = InitTestData(db);

			var authorTable = db.GetTable<Author>();

			var query1 = 
				from a in authorTable.LoadWith(a => a.Books).ThenLoad(b => b.Authors)
				from b in a.Books.OfType<Roman>()
				select new
				{
					Id = b.BookId,
					b.BookName,
					Authors = b.Authors.ToList()
				};

			var query2 = 
				from a in authorTable.LoadWith(a => a.Books).ThenLoad(b => b.Authors)
				from b in a.Books.OfType<Novel>()
				select new
				{
					Id = b.BookId,
					b.BookName,
					Authors = b.Authors.ToList()
				};

			var query = Combine(query1, query2, operation);

			AssertQuery(query);
		}

		[Test]
		public void EagerDifferentDetails([DataSources] string context, [Values(SetOperation.UnionAll, SetOperation.Except, SetOperation.ExceptAll)] SetOperation operation)
		{
			using var db       = GetDataContext(context);
			using var disposal = InitTestData(db);

			var authorTable = db.GetTable<Author>();

			var query1 = 
				from a in authorTable.LoadWith(a => a.Books).ThenLoad(b => b.Authors)
				from b in a.Books.OfType<Roman>()
				select new
				{
					Id = b.BookId,
					b.BookName,
					Authors = b.Authors.ToList()
				};

			var query2 = 
				from a in authorTable.LoadWith(a => a.Books).ThenLoad(b => b.Authors)
				from b in a.Books.OfType<Novel>()
				select new
				{
					Id = b.BookId,
					b.BookName,
					Authors = b.Authors.Where(a => a.AuthorName != "A").ToList()
				};

			var query = Combine(query1, query2, operation);

			AssertQuery(query);
		}

		[Test]
		public void ConcatEagerDifferentDetails([DataSources] string context)
		{
			using var db       = GetDataContext(context);
			using var disposal = InitTestData(db);

			var authorTable = db.GetTable<Author>();

			var query1 = 
				from a in authorTable.LoadWith(a => a.Books).ThenLoad(b => b.Authors)
				from b in a.Books.OfType<Roman>()
				select new
				{
					Id = b.BookId,
					b.BookName,
					Authors = b.Authors.Where(x => x.AuthorName != "A").ToList()
				};

			var query2 = 
				from a in authorTable.LoadWith(a => a.Books).ThenLoad(b => b.Authors)
				from b in a.Books.OfType<Novel>()
				select new
				{
					Id = b.BookId,
					b.BookName,
					Authors = b.Authors.ToList()
				};

			var query = query1.Concat(query2);

			AssertQuery(query);
		}


		[Test]
		public void UsingDictionary([DataSources] string context, [Values] SetOperation operation)
		{
			using var db       = GetDataContext(context);
			using var disposal = InitTestData(db);

			var authorTable = db.GetTable<Author>();

			var query1 = 
				from a in authorTable.LoadWith(a => a.Books).ThenLoad(b => b.Authors)
				from b in a.Books.OfType<Roman>()
				select 
					new Dictionary<string, string>{
						{"Discriminator", b.Discriminator},
						{b.BookName, b.BookName}
					};

			var query2 = 
				from a in authorTable.LoadWith(a => a.Books).ThenLoad(b => b.Authors)
				from b in a.Books.OfType<Novel>()
			select 
				new Dictionary<string, string>{
						{"Discriminator", b.Discriminator},
						{b.BookName, b.BookName}
					};

			var query = Combine(query1, query2, operation);

			AssertQuery(query, new DictionaryEqualityComparer<string, string>());
		}

		class ByBookTypeResult
		{
			public string?       BookType { get; set; }
			public List<Author>? Authors  { get; set; }

			sealed class BookTypeEqualityComparer : IEqualityComparer<ByBookTypeResult>
			{
				public bool Equals(ByBookTypeResult? x, ByBookTypeResult? y)
				{
					if (ReferenceEquals(x, y))
					{
						return true;
					}

					if (ReferenceEquals(x, null))
					{
						return false;
					}

					if (ReferenceEquals(y, null))
					{
						return false;
					}

					if (x.GetType()   != y.GetType())
					{
						return false;
					}

					return x.BookType == y.BookType;
				}

				public int GetHashCode(ByBookTypeResult obj)
				{
					return (obj.BookType != null ? obj.BookType.GetHashCode() : 0);
				}
			}

			public static IEqualityComparer<ByBookTypeResult> BookTypeComparer { get; } = new BookTypeEqualityComparer();
		}

		[Test]
		public void EagerConcatWithSetsDifferentByConstants([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db       = GetDataContext(context);
			using var disposal = InitTestData(db);

			var authorTable = db.GetTable<Author>();

			var query1 =
				from a in authorTable.LoadWith(a => a.Books).ThenLoad(b => b.Authors)
				from b in a.Books.OfType<Roman>()
				select new ByBookTypeResult
				{
					BookType = "Roman", 
					Authors = b.Authors.ToList()
				};

			var query2 = 
				from a in authorTable.LoadWith(a => a.Books).ThenLoad(b => b.Authors)
				from b in a.Books.OfType<Novel>()
				select new ByBookTypeResult
				{
					BookType = "Novel", 
					Authors = b.Authors.Take(2).ToList()
				};

			var query = Combine(query1, query2, SetOperation.UnionAll);

			AssertQuery(query, ByBookTypeResult.BookTypeComparer);
		}

		[Test]
		public void EagerConcatWithSetsDifferentNullability([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db       = GetDataContext(context);
			using var disposal = InitTestData(db);

			var authorTable = db.GetTable<Author>();

			var query1 =
				from a in authorTable.LoadWith(a => a.Books).ThenLoad(b => b.Authors)
				select new 
				{
					AuthorName = a.AuthorName, 
					Books = a.Books.ToList()
				};

			var query2 = 
				from a in authorTable.LoadWith(a => a.Books).ThenLoad(b => b.Authors)
				from b in a.Books.OfType<Novel>()
				select new 
				{
					AuthorName = (string?)null, 
					Books      = a.Books.Take(2).ToList()
				};

			var query = Combine(query1, query2, SetOperation.UnionAll);

			AssertQuery(query);
		}

	}
}
