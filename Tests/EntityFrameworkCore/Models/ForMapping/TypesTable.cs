using System;
using System.ComponentModel.DataAnnotations;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping
{
	public class TypesTable
	{
		[Key]
		public int Id { get; set; }

		public DateTime? DateTime { get; set; }
		public string? String { get; set; }

		// add more if needed for tests
	}
}
