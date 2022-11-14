using System;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Npgsql;
using NpgsqlTypes;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3548Tests : TestBase
	{
		[Table]
		public partial class User
		{
			[PrimaryKey                                                 ] public int          Id             { get; set; }
			[Column(DataType = DataType.Enum, DbType = "user_type_enum")] public UserTypeEnum Type           { get; set; }
			[Column                                                     ] public int?         OrganizationId { get; set; }

			[ExpressionMethod(nameof(InYourOrganizationSelector))]
			public bool InYourOrganization(User callingUser) => throw new NotImplementedException();

			static Expression<Func<User, User, bool>> InYourOrganizationSelector()
			{
				return (user, callingUser) => (user.Type == UserTypeEnum.Organization && user.Id == callingUser.Id)
						|| (user.Type == UserTypeEnum.Organization && callingUser.Type == UserTypeEnum.OrganizationUser && user.Id == callingUser.OrganizationId)
						|| (user.Type == UserTypeEnum.OrganizationUser && callingUser.Type == UserTypeEnum.Organization && user.OrganizationId == callingUser.Id)
						|| (user.Type == UserTypeEnum.OrganizationUser && callingUser.Type == UserTypeEnum.OrganizationUser && user.OrganizationId == callingUser.OrganizationId);
			}

			public static readonly User[] Data = new[]
			{
				new User() { Id = 1, Type = UserTypeEnum.Organization },
				new User() { Id = 2, Type = UserTypeEnum.Organization },
				new User() { Id = 3, Type = UserTypeEnum.OrganizationUser, OrganizationId = 1 },
				new User() { Id = 4, Type = UserTypeEnum.OrganizationUser, OrganizationId = 2 },
			};
		}

		[PgName("user_type_enum")]
		public enum UserTypeEnum
		{
			[PgName("org")]
			[MapValue("org")]
			Organization = 1,

			[PgName("org_user")]
			[MapValue("org_user")]
			OrganizationUser = 2,
		}

		[Test]
		public void EnumEvaluationInFilter([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataConnection(context))
				db.Execute("DROP TABLE IF EXISTS \"User\";DROP TYPE IF EXISTS user_type_enum;CREATE TYPE user_type_enum AS ENUM('org', 'org_user');");

			// TODO: currently unclear how to integrate new API with linq2db, will address in https://github.com/linq2db/linq2db/issues/3501
#pragma warning disable CS0618 // Type or member is obsolete
			NpgsqlConnection.GlobalTypeMapper.MapEnum<UserTypeEnum>();
#pragma warning restore CS0618 // Type or member is obsolete

			try
			{
				using var db = GetDataConnection(context);
				using var _  = db.CreateLocalTable(User.Data);

				((NpgsqlConnection)db.Connection).ReloadTypes();

				var user  = new User() { Id = 1, Type = UserTypeEnum.Organization };
				var users = db.GetTable<User>().Where(x => x.InYourOrganization(user)).OrderBy(x => x.Id).Select(x => x.Id.ToString()).ToList();

				Assert.AreEqual(2, users.Count);
				Assert.AreEqual("1", users[0]);
				Assert.AreEqual("3", users[1]);

				user  = new User() { Id = 4, Type = UserTypeEnum.OrganizationUser, OrganizationId = 2 };
				users = db.GetTable<User>().Where(x => x.InYourOrganization(user)).OrderBy(x => x.Id).Select(x => x.Id.ToString()).ToList();

				Assert.AreEqual(2, users.Count);
				Assert.AreEqual("2", users[0]);
				Assert.AreEqual("4", users[1]);
			}
			finally
			{
				using (var db = GetDataConnection(context))
				{
					try
					{
						db.Execute("DROP TYPE IF EXISTS user_type_enum;");
					}
					catch 
					{
					}
				}
			}
		}
	}
}
