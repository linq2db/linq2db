using System;

using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.Mappings
{
	[Table("User")]
	public class User
	{
		[Column("id", IsPrimaryKey = true, IsIdentity = true)] public long    Id          { get; set; }
		[Column("name")]                                       public string? Name        { get; set; }
		[Column("login_count")]                                public int?    Login_count { get; set; }
	}

	[Table]
	public class Workflow
	{
		[PrimaryKey] public int Id { get; set; }

		[Column] public int RowVersion { get; set; }

		[Column(DataType = DataType.VarChar, Length = 10)]
		public StatusEnum Status { get; set; }

		[Column] public string? Result { get; set; }
		[Column] public string? Error  { get; set; }
		[Column] public string? Steps  { get; set; }

		[Column] public DateTimeOffset StartTime     { get; set; }
		[Column] public DateTimeOffset UpdateTime    { get; set; }
		[Column] public DateTimeOffset ProcessedTime { get; set; }
		[Column] public DateTimeOffset CompleteTime  { get; set; }
	}

	public enum StatusEnum
	{
		[MapValue("ONE")]
		One,
		[MapValue("THREE")]
		Two,
		[MapValue("FOUR")]
		Three,
	}
}
