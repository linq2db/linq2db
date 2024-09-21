using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using NpgsqlTypes;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel
{
	public class Parent
	{
		public int Id { get; set; }
		public int? ParentId { get; set; }

		public Parent ParentsParent { get; set; } = null!;
		public ICollection<Parent> ParentChildren { get; set; } = null!;
		public ICollection<Child> Children { get; set; } = null!;
	}

	public class Child
	{
		public int Id { get; set; }
		public int ParentId { get; set; }

		public Parent Parent { get; set; } = null!;
		public ICollection<GrandChild> GrandChildren { get; set; } = null!;
	}

	public class GrandChild
	{
		public int Id { get; set; }
		public int ChildId { get; set; }

		public Child Child { get; set; } = null!;
	}

	public class ShadowTable
	{
		public int Id { get; set; }
	}

	public class IdentityTable
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int Id { get; private set; }
		[MaxLength(50)] public required string Name { get; init; }
	}

	#region Issue 4624
	public interface IIssue4624EntityWithIdBase<T>
	{
		T Id { get; set; }
	}

	public interface IIssue4624EntityWithId : IIssue4624EntityWithIdBase<int>
	{ }

	public partial class Issue4624ItemTicketDate
	{
		public int Id { get; set; }
		public int ItemId { get; set; }
		public int EntryCount { get; set; }
		public int TicketDateId { get; set; }
		public string? Message { get; set; }
		public string? MessageBackcolor { get; set; }
		public string? MessageForecolor { get; set; }
		public string? DisplayInfo { get; set; }
		public Issue4624Item Item { get; set; } = null!;
	}

	public partial class Issue4624Entry : IIssue4624EntityWithId
	{
		public int AclItemId { get; set; }
		public int EntriesCount { get; set; }
		public int TicketNumberId { get; set; }
		public int Id { get; set; }

		public Issue4624Item Item { get; set; } = null!;
	}

	public partial class Issue4624Item : IIssue4624EntityWithId
	{
		public Issue4624Item()
		{
			ItemTicketDates = new HashSet<Issue4624ItemTicketDate>();
			Entries = new HashSet<Issue4624Entry>();
		}

		public Issue4624Item(int id) : this()
		{
			Id = id;
		}


		public int Id { get; set; }
		public DateTime? DateFrom { get; set; }
		public DateTime? DateTo { get; set; }
		public string? Name { get; set; }
		public int AclNameId { get; set; }
		public bool SendCustomFields { get; set; }

		public int? Capacity { get; set; }
		public bool CfDriven { get; set; }
		public string? CfAllowField { get; set; }
		public string? CfAllowValue { get; set; }
		public string? CfDisallowField { get; set; }
		public string? CfDisallowValue { get; set; }

		public int? CreditGroupId { get; set; }

		public ICollection<Issue4624ItemTicketDate> ItemTicketDates { get; set; }
		public ICollection<Issue4624Entry> Entries { get; set; }
	}
	#endregion

	public class Master
	{
		public int Id { get; set; }

		public ICollection<Detail> Details { get; set; } = null!;
	}

	public class Detail
	{
		public int Id { get; set; }

		public int MasterId { get; set; }
		public Master Master { get; set; } = null!;
	}
}
