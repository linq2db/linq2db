namespace LinqToDB.EntityFrameworkCore.Tests.Models.JsonConverter
{
	public class EventScheduleItemBase
	{
		public int Id { get; set; }
		public virtual LocalizedString NameLocalized { get; set; } = null!;
		public virtual string? JsonColumn { get; set; }
	}
}
