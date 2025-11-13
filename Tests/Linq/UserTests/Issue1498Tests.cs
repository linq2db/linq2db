using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1498Tests : TestBase
	{
		public class Topic
		{
			[PrimaryKey] public int Id { get; set; }

			public string? Title { get; set; }

			public string? Text { get; set; }

			[Association(ThisKey = "Id", OtherKey = "TopicId")]
			public virtual ICollection<Message> MessagesA1 { get; set; } = null!;

			[Association(ExpressionPredicate = nameof(Predicate))]
			public virtual ICollection<Message> MessagesA2 { get; set; } = null!;

			[Association(QueryExpressionMethod = nameof(Query))]
			public virtual ICollection<Message> MessagesA3 { get; set; } = null!;

			public virtual ICollection<Message> MessagesF1 { get; set; } = null!;
			public virtual ICollection<Message> MessagesF2 { get; set; } = null!;
			public virtual ICollection<Message> MessagesF3 { get; set; } = null!;

			static Expression<Func<Topic, Message, bool>> Predicate => (t, m) => t.Id == m.TopicId;

			static Expression<Func<Topic, IDataContext, IQueryable<Message>>> Query => (t, ctx) => ctx.GetTable<Message>().Where(m => m.TopicId == t.Id);
		}

		public class Message
		{
			[PrimaryKey] public int Id { get; set; }

			public int TopicId { get; set; }

			[Association(ThisKey = "TopicId", OtherKey = "Id")]
			public virtual Topic? Topic { get; set; }

			public string? Text { get; set; }
		}

		[YdbTableNotFound]
		[Test]
		public void TestAttributesByKey([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Topic>())
			using (db.CreateLocalTable<Message>())
			{
				db.Insert(new Topic() { Id = 6, Title = "title", Text = "text" });

				db.GetTable<Topic>()
					.Where(x => x.Id == 6)
					.Select(x =>
					new
					{
						Topic = x,
						MessagesIds = x.MessagesA1.Select(t => t.Id).ToList()
					}).FirstOrDefault();
			}
		}

		[YdbTableNotFound]
		[Test]
		public void TestAttributesByExpression([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Topic>())
			using (db.CreateLocalTable<Message>())
			{
				db.Insert(new Topic() { Id = 6, Title = "title", Text = "text" });

				db.GetTable<Topic>()
					.Where(x => x.Id == 6)
					.Select(x =>
					new
					{
						Topic = x,
						MessagesIds = x.MessagesA2.Select(t => t.Id).ToList()
					}).FirstOrDefault();
			}
		}

		[YdbTableNotFound]
		[Test]
		public void TestAttributesByQuery([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Topic>())
			using (db.CreateLocalTable<Message>())
			{
				db.Insert(new Topic() { Id = 6, Title = "title", Text = "text" });

				db.GetTable<Topic>()
					.Where(x => x.Id == 6)
					.Select(x =>
					new
					{
						Topic = x,
						MessagesIds = x.MessagesA3.Select(t => t.Id).ToList()
					}).FirstOrDefault();
			}
		}

		[YdbTableNotFound]
		[Test]
		public void TestFluentAssociationByExpression([DataSources] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<Topic>()
					.Property(e => e.Id)
					.Association(e => e.MessagesF1, (t, m) => t.Id == m.TopicId)
					.Property(e => e.Title)
					.Property(e => e.Text)
				.Entity<Message>()
					.Property(e => e.Id)
					.Property(e => e.TopicId)
					.Property(e => e.Text)
				.Build();

			using (var db = GetDataContext(context, ms))
			{
				using (db.CreateLocalTable<Topic>())
				using (db.CreateLocalTable<Message>())
				{
					db.Insert(new Topic() { Id = 6, Title = "title", Text = "text" });

					db.GetTable<Topic>()
						.Where(x => x.Id == 6)
						.Select(x =>
						new
						{
							Topic = x,
							MessagesIds = x.MessagesF1.Select(t => t.Id).ToList()
						}).FirstOrDefault();
				}
			}
		}

		[YdbTableNotFound]
		[Test]
		public void TestFluentAssociationByKeys([DataSources] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<Topic>()
					.Property(e => e.Id)
					.Association(e => e.MessagesF2, t => t.Id, m => m.TopicId)
					.Property(e => e.Title)
					.Property(e => e.Text)
				.Entity<Message>()
					.Property(e => e.Id)
					.Property(e => e.TopicId)
					.Property(e => e.Text)
				.Build();

			using (var db = GetDataContext(context, ms))
			{
				using (db.CreateLocalTable<Topic>())
				using (db.CreateLocalTable<Message>())
				{
					db.Insert(new Topic() { Id = 6, Title = "title", Text = "text" });

					db.GetTable<Topic>()
						.Where(x => x.Id == 6)
						.Select(x =>
						new
						{
							Topic = x,
							MessagesIds = x.MessagesF2.Select(t => t.Id).ToList()
						}).FirstOrDefault();
				}
			}
		}

		[YdbTableNotFound]
		[Test]
		public void TestFluentAssociationByQuery([DataSources] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<Topic>()
					.Property(e => e.Id)
					.Association(e => e.MessagesF3, (t, ctx) => ctx.GetTable<Message>().Where(m => m.TopicId == t.Id))
					.Property(e => e.Title)
					.Property(e => e.Text)
				.Entity<Message>()
					.Property(e => e.Id)
					.Property(e => e.TopicId)
					.Property(e => e.Text)
				.Build();

			using (var db = GetDataContext(context, ms))
			{
				using (db.CreateLocalTable<Topic>())
				using (db.CreateLocalTable<Message>())
				{
					db.Insert(new Topic() { Id = 6, Title = "title", Text = "text" });
					db.Insert(new Message() { Id = 60, Text = "message", TopicId = 6});
					db.Insert(new Message() { Id = 61, Text = "message", TopicId = 7});

					var result = db.GetTable<Topic>()
						.Where(x => x.Id == 6)
						.Select(x =>
						new
						{
							Topic = x,
							MessagesIds = x.MessagesF3.Select(t => t.Id).ToList()
						}).FirstOrDefault()!;

					Assert.That(result, Is.Not.Null);
					Assert.That(result.MessagesIds.Single(), Is.EqualTo(60));
				}
			}
		}

		[YdbTableNotFound]
		[Test]
		public void TestFluentAssociationByQueryWithKeys([DataSources] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<Topic>()
					.Property(e => e.Id).IsPrimaryKey()
					.Association(e => e.MessagesF3, (t, ctx) => ctx.GetTable<Message>().Where(m => m.TopicId == t.Id))
					.Property(e => e.Title)
					.Property(e => e.Text)
				.Entity<Message>()
					.Property(e => e.Id).IsPrimaryKey()
					.Property(e => e.TopicId)
					.Property(e => e.Text)
				.Build();

			using (var db = GetDataContext(context, ms))
			{
				using (db.CreateLocalTable<Topic>())
				using (db.CreateLocalTable<Message>())
				{
					var topic = new Topic { Id = 6, Text = "text", Title = "title" };

					db.Insert(topic);
					db.Insert(new Message { Id = 60, Text = "message", TopicId = 6});
					db.Insert(new Message { Id = 61, Text = "message", TopicId = 7});

					var result = db.GetTable<Topic>()
						.Where(x => x.Id == 6)
						.Select(x =>
						new
						{
							Topic = x,
							MessagesIds = x.MessagesF3.Select(t => t.Id).ToList()
						}).FirstOrDefault()!;
					using (Assert.EnterMultipleScope())
					{
						Assert.That(result, Is.Not.Null);
						Assert.That(topic.Id, Is.EqualTo(result.Topic.Id));
						Assert.That(topic.Text, Is.EqualTo(result.Topic.Text));
						Assert.That(topic.Title, Is.EqualTo(result.Topic.Title));
						Assert.That(new[] { 60 }, Is.EqualTo(result.MessagesIds));
					}
				}
			}
		}
	}
}
