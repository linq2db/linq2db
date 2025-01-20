using System.ComponentModel.DataAnnotations;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping
{
	public class StringTypes
	{
		[Key]
		public int Id { get; set; }

		public string? AsciiString { get; set; }

		public string? UnicodeString { get; set; }
	}
}
