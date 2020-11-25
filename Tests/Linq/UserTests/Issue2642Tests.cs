using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2642Tests : TestBase
	{
		[Table("mails")]
		class Email
		{
			[Column]
			public int Id { get; set; }
			[Column]
			public DateTime AddTime { get; set; }
			[Association(ThisKey = "Id", OtherKey = "EmailId")]
			public IEnumerable<EmailAttachment> Attachments { get; set; } = null!;
		}

		[Table("EmailAttachments")]
		class EmailAttachment
		{
			[Column]
			public int Id { get; set; }
			[Column]
			public int EmailId { get; set; }
			[Column]
			public string Data { get; set; } = null!;
		}

		[Table("IIRs")]
		class Iir
		{
			[Column]
			public int Id { get; set; }
		}

		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Email>())
			using (db.CreateLocalTable<EmailAttachment>())
			using (db.CreateLocalTable<Iir>())
			{
				var query = from p in db.GetTable<Email>().LoadWith(c => c.Attachments)
					join i in db.GetTable<Iir>() on p.Id equals i.Id
					where p.AddTime > DateTime.Now
					orderby p.AddTime
					select p;

				var result = query
					.ToArray();
			}
		}
	}
}
