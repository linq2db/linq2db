using LinqToDB.Mapping;

namespace Tests.Model
{
	public interface IIssue4031
	{
		public int Id { get; set; }
	}

	public interface IIssue4031<T>
	{
		public T Id { get; set; }
	}

	// must implement IIssue4031 without specifying IIssue4031 in implementation list
	[Table("Person")]
	public class Issue4031BaseExternal
	{
		[Column("PersonID")] public int Id { get; set; }
	}
}
