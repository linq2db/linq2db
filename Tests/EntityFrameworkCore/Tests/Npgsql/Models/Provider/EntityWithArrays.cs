using System;
using System.ComponentModel.DataAnnotations;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models
{
	public class EntityWithArrays
	{
		[Key]
		public int Id { get; set; }

		public Guid[] Guids { get; set; } = null!;
	}
}
