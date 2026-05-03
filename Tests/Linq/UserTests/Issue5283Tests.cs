using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	public class Issue5283Tests : TestBase
	{
		static class SettingNameIds
		{
			public const string UserNumberMaxValueId = "UserNumberMaxValue";
		}

		class User
		{
			public int    Id         { get; set; }
			public int    HospitalId { get; set; }
			public string Name       { get; set; } = null!;
			public int    UserNumber { get; set; }

			[Association(ThisKey = nameof(HospitalId), OtherKey = nameof(Hospital.Id), CanBeNull = true)]
			public Hospital Hospital { get; set; } = null!;
		}

		public class Hospital
		{
			public int    Id   { get; set; }
			public string Name { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(HospitalSetting.HospitalId), CanBeNull = true)]
			public List<HospitalSetting> Settings { get; set; } = null!;

			[ExpressionMethod(nameof(GetSettingValueImpl))]
			public string? GetSettingValue(string settingId) => throw new System.NotImplementedException();

			static Expression<Func<Hospital, string, string?>> GetSettingValueImpl()
			{
				return (h, settingId) => h.Settings
					.Where(sv => sv.SettingId == settingId)
					.Select(sv => sv.SettingValue)
					.SingleOrDefault();
			}

			[ExpressionMethod(nameof(GetUserNumberMaxValueImpl))]
			public int GetUserNumberMaxValue() => throw new System.NotImplementedException();

			static Expression<Func<Hospital, int>> GetUserNumberMaxValueImpl()
			{
				return h => Sql.ConvertTo<int?>.From(h.GetSettingValue(SettingNameIds.UserNumberMaxValueId)) ?? 9000;
			}
		}

		[ExpressionMethod(nameof(FormatUserFromUserNumberImpl))]
		static string FormatUserNumber(User user)
		{
			throw new System.NotImplementedException();
		}

		static Expression<Func<User, string>> FormatUserFromUserNumberImpl()
		{
			return u => u.UserNumber >= 0 && u.UserNumber.ToString().Length < u.Hospital.GetUserNumberMaxValue().ToString().Length
				? u.UserNumber.ToString().PadLeft(u.Hospital.GetUserNumberMaxValue().ToString().Length, '0')
				: u.UserNumber.ToString();
		}

		public class HospitalSetting
		{
			public int    Id           { get; set; }
			public int    HospitalId   { get; set; }
			public string SettingId    { get; set; } = null!;
			public string SettingValue { get; set; } = null!;

			[Association(ThisKey = nameof(HospitalId), OtherKey = nameof(Hospital.Id), CanBeNull = true)]
			public Hospital Hospital { get; set; } = null!;
		}

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var userTable = db.CreateLocalTable(new[]
			{
				new User { Id = 1, HospitalId = 1, Name = "User 1", UserNumber = 1 },
				new User { Id = 2, HospitalId = 1, Name = "User 2", UserNumber = 2 },
				new User { Id = 3, HospitalId = 1, Name = "User 3", UserNumber = 3 },
				new User { Id = 4, HospitalId = 2, Name = "User 4", UserNumber = 9001 },
				new User { Id = 5, HospitalId = 2, Name = "User 5", UserNumber = 9002 },
				new User { Id = 6, HospitalId = 2, Name = "User 6", UserNumber = 9003 }
			});
			using var hospitalTable = db.CreateLocalTable(new[]
			{
				new Hospital { Id = 1, Name = "Hospital 1" },
				new Hospital { Id = 2, Name = "Hospital 2" }
			});
			using var hospitalSettingTable = db.CreateLocalTable(new[]
			{
				new HospitalSetting { Id = 1, HospitalId = 1, SettingId = SettingNameIds.UserNumberMaxValueId, SettingValue = "100" },
				new HospitalSetting { Id = 2, HospitalId = 2, SettingId = SettingNameIds.UserNumberMaxValueId, SettingValue = "10000" }
			});

			var searchLexems = new[] { "001", "002", "003", "9001", "9002", "9003" };

			var query =
				from user in db.GetTable<User>()
				select new
				{
					Data = new
					{
						UserNumberFormatted = FormatUserNumber(user)
					}
				};

			query = query.Where(u => searchLexems.Any(l => l == u.Data.UserNumberFormatted));

			var result = query.ToList();

			result.Count.ShouldBe(3);
		}

	}
}
