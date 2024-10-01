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
		public bool IsActive { get; set; }
		public int ParentId { get; set; }
		public string? Name { get; set; }

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

	public class TypesTable
	{
		public int Id { get; set; }
		public DateTimeOffset DateTimeOffset { get; set; }
		public DateTimeOffset? DateTimeOffsetN { get; set; }
		public DateTimeOffset DateTimeOffsetWithConverter { get; set; }
		public DateTimeOffset? DateTimeOffsetNWithConverter { get; set; }
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

	#region Issue 122
	public class Issue4627Container
	{
		public int Id { get; set; }
		public virtual ICollection<Issue4627Item> Items { get; set; } = null!;
		public virtual ICollection<Issue4627ChildItem> ChildItems { get; set; } = null!;
	}

	public class Issue4627Item
	{
		public int Id { get; set; }
		public int ContainerId { get; set; }
		public virtual Issue4627ChildItem Child { get; set; } = null!;
		public virtual Issue4627Container Container { get; set; } = null!;
	}

	public class Issue4627ChildItem
	{
		public int Id { get; set; }
		public virtual Issue4627Item Parent { get; set; } = null!;
	}
	#endregion

	#region Issue 4628
	public class Issue4628Base
	{
		public int Id { get; set; }
		[ForeignKey("Other")]
		public int OtherId { get; set; }
		public Issue4628Other Other { get; set; } = null!;
	}

	public class Issue4628Inherited : Issue4628Base
	{
		public string? SomeValue { get; set; }
	}

	public class Issue4628Other
	{
		public int Id { get; set; }
		[InverseProperty("Other")]
		public virtual ICollection<Issue4628Base> Values { get; set; } = null!;
	}
	#endregion

	#region Issue 4629
	public class Issue4629Post
	{
		public int Id { get; set; }

		public virtual ICollection<Issue4629Tag> Tags { get; set; } = null!;
	}

	public class Issue4629Tag
	{
		public int Id { get; set; }
		public int PostId { get; set; }
		public int Weight { get; set; }

		public Issue4629Post Post { get; set; } = null!;
	}
	#endregion

	#region Issue 340

	public abstract class Issue340BaseEntity
	{
		public virtual Guid Id { get; set; }

		public bool IsActive { get; set; } = true;
	}

	public class Issue340Entity : Issue340BaseEntity
	{
		[Key]
		public new virtual long Id { get; set; }

		public new bool? IsActive { get; set; } = true;
	}

	#endregion

	#region Issue 4640
	public class Issue4640Table
	{
		public int Id { get; set; }
		public List<Issue4640Items>? Items { get; set; }
	}

	public class Issue4640Items
	{
		public string? Name { get; set; }

		public int? Offset { get; set; }
	}
	#endregion

	public class Issue212Table
	{
		public int Id { get; set; }
		public string? Value { get; set; }
		public DateTime Timestamp { get; set; }
	}

	#region Issue 4642
	public class Issue4642Table1
	{
		public int Id { get; set; }
	}

	public class Issue4642Table2
	{
		public int Id { get; set; }

		public string SystemId { get; set; } = null!;

		public DateTime Timestamp { get; set; }
	}
	#endregion

	#region Issue 4644
	public abstract class Issue4644EntityBase
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public DateTime CreateDate { get; set; } = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(3)).DateTime;
	}
	public class Issue4644Main : Issue4644EntityBase
	{
		[StringLength(100)]
		public string? Name { get; set; }
		[InverseProperty(nameof(Issue4644BaseItem.Main))]
		public Issue4644BaseItem? Details { get; set; }
	}

	public class Issue4644BaseItem : Issue4644EntityBase
	{
		public int MainId { get; set; }
		[ForeignKey(nameof(MainId))]
		public Issue4644Main? Main { get; set; }
	}

	public class Issue4644PricedItem : Issue4644BaseItem
	{
		public decimal Price { get; set; }
	}
	#endregion

	public class Issue4649Table
	{
		public int Id { get; set; }
		public string? Name { get; set; }
	}

	public class Issue261Table
	{
		public int Id { get; set; }
		public string? Name { get; set; }
	}
}
