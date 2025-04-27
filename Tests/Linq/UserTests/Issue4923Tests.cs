using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue4923Tests : TestBase
	{
		public class Department
		{
			[Column(IsPrimaryKey = true)]
			public int    Id   { get; set; }
			[Column]
			public string Name { get; set; } = null!;
		}

		public class User
		{
			[Column(IsPrimaryKey = true, IsIdentity = true)]
			public int    Id           { get; set; }
			[Column]
			public string Name         { get; set; } = null!;
			[Column]
			public int    DepartmentId { get; set; }
		}

		public class UserDto
		{
			public string Name           { get; set; } = null!;
			public string DepartmentName { get; set; } = null!;
		}

		[Test]
		public void MergeWithSubquery([IncludeDataSources(TestProvName.AllPostgreSQL15Plus)] string context)
		{
			using var db = GetDataConnection(context);

			var users = new[]
			{
				new UserDto { Name = "U1", DepartmentName = "D1"}
			};

			var departments = new[]
			{
				new Department {Id = 1, Name = "D1"},
				new Department {Id = 2, Name = "D2"}
			};

			using var dt = db.CreateLocalTable<Department>(departments);
			using var ut = db.CreateLocalTable<User>();

			db.GetTable<User>()
				.Merge()
				.Using(users)
				.On(u => u.Name, dto => dto.Name)
				.InsertWhenNotMatched(dto => new User
				{
					Name = dto.Name,
					DepartmentId = db.GetTable<Department>()
						.Where(d => d.Name == dto.DepartmentName)
						.Select(x => x.Id)
						.First()

				})
				.UpdateWhenMatched((_, dto) => new User
				{
					DepartmentId = db.GetTable<Department>()
						.Where(d => d.Name == dto.DepartmentName)
						.Select(x => x.Id)
						.First()
				})
				.Merge();
		}
	}
}
