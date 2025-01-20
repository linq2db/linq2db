using System.ComponentModel.DataAnnotations;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel
{
	public class Patent
	{
		[Key]
		public int Id { get; set; }
		public PatentAssessment? Assessment { get; set; }
	}

	public class PatentAssessment
	{
		[Key]
		public int PatentId { get; set; }
		public Patent Patent { get; set; } = null!;
		public int? TechnicalReviewerId { get; set; }
	}
}
