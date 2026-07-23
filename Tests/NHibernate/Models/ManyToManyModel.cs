using System.Collections.Generic;

using FluentNHibernate.Mapping;

namespace LinqToDB.NHibernate.Tests.Models.ManyToMany
{
	public class Author
	{
		public Author()
		{
			Books = new HashSet<Book>();
		}

		public virtual int                Id    { get; set; }
		public virtual string             Name  { get; set; } = null!;
		public virtual ICollection<Book>  Books { get; set; }
	}

	public class Book
	{
		public virtual int    Id    { get; set; }
		public virtual string Title { get; set; } = null!;
	}

	// The many-to-many join table, mapped as an entity so linq2db can query it directly.
	// NHibernate requires composite-id entities to override Equals/GetHashCode.
	public class AuthorBook
	{
		public virtual int AuthorId { get; set; }
		public virtual int BookId   { get; set; }

		public override bool Equals(object? obj)
			=> obj is AuthorBook other && other.AuthorId == AuthorId && other.BookId == BookId;

		public override int GetHashCode()
			=> (AuthorId, BookId).GetHashCode();
	}

	public class AuthorMap : ClassMap<Author>
	{
		public AuthorMap()
		{
			Table("Authors");
			Id(x => x.Id).GeneratedBy.Identity().Column("Id");
			Map(x => x.Name).Column("Name").Not.Nullable();
			HasManyToMany(x => x.Books).Table("AuthorBook").ParentKeyColumn("AuthorId").ChildKeyColumn("BookId");
		}
	}

	public class BookMap : ClassMap<Book>
	{
		public BookMap()
		{
			Table("Books");
			Id(x => x.Id).GeneratedBy.Identity().Column("Id");
			Map(x => x.Title).Column("Title").Not.Nullable();
		}
	}

	public class AuthorBookMap : ClassMap<AuthorBook>
	{
		public AuthorBookMap()
		{
			Table("AuthorBook");
			CompositeId()
				.KeyProperty(x => x.AuthorId, "AuthorId")
				.KeyProperty(x => x.BookId,   "BookId");
		}
	}
}
