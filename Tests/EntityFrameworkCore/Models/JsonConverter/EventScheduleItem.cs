using System;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.JsonConverter
{
	public sealed class EventScheduleItem : EventScheduleItemBase
	{
		public CrashEnum CrashEnum { get; set; }
		public Guid GuidColumn { get; set; }
	}
}
