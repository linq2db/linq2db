using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2602Tests : TestBase
	{
		[Table(Name = "Emails")]
		public class Email
		{
			[Identity]
			[PrimaryKey]
			public int Id { get; set; }

			[Association(ThisKey = "Id", OtherKey = "EmailId")]
			public IEnumerable<EmailAttachment> Attachments { get; set; } = null!;
		}

		[Table(Name = "EmailAttachment")]
		public class EmailAttachment
		{
			[Identity]
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			[NotNull]
			public int EmailId { get; set; }

			[Column]
			[NotNull]
			public string FileName { get; set; } = null!;
		}

		private sealed class EmailReader : IDisposable
		{
			private bool disposed;
			private readonly int id;

			public EmailReader(int id)
			{
				this.id = id;
			}

			public void Dispose()
			{
				disposed = true;
			}

			public int GetId()
			{
				if (disposed)
				{
					throw new ObjectDisposedException("Use after dispose"); // Crashed here on 2nd call to GetEmail
				}

				return id;
			}

			public Email? GetEmail(string context)
			{
				using (var db = new DataConnection(context))
				{
					return db.GetTable<Email>().LoadWith(c => c.Attachments).FirstOrDefault(c => c.Id == GetId());
				}
			}
		}

		[Test]
		public void TestParameterCaching([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Email>())
			using (db.CreateLocalTable<EmailAttachment>())
			{
				var reader1 = new EmailReader(35);
				reader1.GetEmail(context);
				reader1.Dispose();
				using var reader2 = new EmailReader(36);

				Assert.DoesNotThrow(() => reader2.GetEmail(context));
			}
		}
	}
}
