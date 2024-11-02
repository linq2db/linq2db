using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2895Tests : TestBase
	{
		[Table]
		public class InternalEmail
		{
			[PrimaryKey, Column]
			public int Id { get; set; }

			[Column]
			public int? RequestId { get; set; }

			[Column]
			public int? UserId { get; set; }

			[Association(ThisKey = nameof(RequestId), OtherKey = nameof(Issue2895Tests.Request.Id))]
			public virtual Request Request { get; set; } = null!;

			[Association(ThisKey = nameof(UserId), OtherKey = nameof(Issue2895Tests.User.Id))]
			public virtual User User { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Issue2895Tests.Email.Id))]
			public virtual Email Email { get; set; } = null!;
		}

		[Table]
		public class Request
		{
			[PrimaryKey, Column]
			public int Id { get; set; }

			[Column]
			public int UserId { get; set; }

			[Association(ThisKey = nameof(UserId), OtherKey = nameof(Issue2895Tests.User.Id))]
			public virtual User User { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(InternalEmail.Id))]
			public virtual ICollection<InternalEmail> InternalEmails { get; set; } = null!;
		}

		[Table]
		public class User
		{
			[PrimaryKey, Column]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Issue2895Tests.Admin.Id))]
			public virtual Admin Admin { get; set; } = null!;
		}

		[Table]
		public class Admin
		{
			[PrimaryKey, Column]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(EmailAdminAssociation.AdminId))]
			public virtual ICollection<EmailAdminAssociation> EmailAdminAssociations { get; set; } = null!;
		}

		[Table]
		public class EmailAdminAssociation
		{
			[PrimaryKey(1)]
			[Column]
			public int EmailId { get; set; }

			[PrimaryKey(2)]
			[Column]
			public int AdminId { get; set; }

			[Association(ThisKey = nameof(EmailId), OtherKey = nameof(Issue2895Tests.Email.Id))]
			public virtual Email Email { get; set; } = null!;

			[Association(ThisKey = nameof(AdminId), OtherKey = nameof(Issue2895Tests.Admin.Id))]
			public virtual Admin Admin { get; set; } = null!;
		}

		[Table]
		public class Email
		{
			[PrimaryKey, Column]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Issue2895Tests.InternalEmail.Id))]
			public virtual InternalEmail InternalEmail { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(EmailAttachmentAssociation.EmailId))]
			public virtual ICollection<EmailAttachmentAssociation> EmailAttachmentAssociations { get; set; } = null!;
		}

		[Table]
		public class EmailAttachmentAssociation
		{
			[PrimaryKey(1)]
			[Column]
			public int EmailId { get; set; }

			[PrimaryKey(2)]
			[Column]
			public int AttachmentId { get; set; }

			[Association(ThisKey = nameof(AttachmentId), OtherKey = nameof(Issue2895Tests.Attachment.Id))]
			public virtual Attachment Attachment { get; set; } = null!;

			[Association(ThisKey = nameof(EmailId), OtherKey = nameof(Issue2895Tests.Email.Id))]
			public virtual Email Email { get; set; } = null!;
		}

		[Table]
		public class Attachment
		{
			[PrimaryKey, Column]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Document.AttachmentId))]
			public virtual ICollection<Document> Documents { get; set; } = null!;
		}

		[Table]
		public class Document
		{
			[PrimaryKey(1)]
			[Column]
			public int AttachmentId { get; set; }

			[PrimaryKey(2)]
			[Column]
			public int Position { get; set; }

			[Column]
			public string Name { get; set; } = null!;
		}

		[Test]
		[ThrowsForProvider(typeof(LinqException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void EagerLoadingTest([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InternalEmail>(new[] { new InternalEmail { Id = 10, UserId = 1, RequestId = 1 } }))
			using (db.CreateLocalTable<Request>(new[] { new Request { Id = 1, UserId = 1 } }))
			using (db.CreateLocalTable<User>(new[] { new User { Id = 1 } }))
			using (db.CreateLocalTable<Admin>(new[] { new Admin { Id = 1 } }))
			using (db.CreateLocalTable<EmailAdminAssociation>(new[] { new EmailAdminAssociation { AdminId = 1, EmailId = 10 } }))
			using (db.CreateLocalTable<Email>(new[] { new Email { Id = 10 } }))
			using (db.CreateLocalTable<EmailAttachmentAssociation>(new[] { new EmailAttachmentAssociation { EmailId = 10, AttachmentId = 100 } }))
			using (db.CreateLocalTable<Attachment>(new[] { new Attachment { Id = 100 } }))
			using (db.CreateLocalTable<Document>(new[]
			{
				new Document{AttachmentId = 100, Name = "Some Doc 1", Position = 1},
				new Document{AttachmentId = 100, Name = "Some Doc 2", Position = 2},
				new Document{AttachmentId = 101, Name = "Some Doc x", Position = 1},
			}))
			{
				var result = db.GetTable<Request>()
					.Select(r => new
					{
						DocumentName = r.User.Admin.EmailAdminAssociations
							.Select(ea => ea.Email.InternalEmail)
							.Select(ie => new
							{
								Names = ie.Email.EmailAttachmentAssociations
									.SelectMany(eba => eba.Attachment.Documents.Select(aa => aa.Name)),
							})
							.FirstOrDefault(),
					})
					.ToArray();

				result.Should().HaveCount(1);
				result[0].DocumentName?.Names.Should().HaveCount(2);
			}
		}
	}
}
