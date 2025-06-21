using System;
using System.Linq;
using System.Linq.Expressions;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
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
			IDataProvider dataProvider;
			string?       connectionString;

			using (var db = GetDataConnection(context))
			{
				db.Execute("DROP TABLE IF EXISTS \"User\";DROP TYPE IF EXISTS user_type_enum;CREATE TYPE user_type_enum AS ENUM('org', 'org_user');");
				dataProvider     = db.DataProvider;
				connectionString = db.ConnectionString;
			}

			var builder = new NpgsqlDataSourceBuilder(connectionString);
			builder.MapEnum<UserTypeEnum>();
			var dataSource = builder.Build();

			var options = new DataOptions()
				.UseConnectionFactory(dataProvider, _ => dataSource.CreateConnection());

			try
			{
				using var db = GetDataConnection(options);
				using var _  = db.CreateLocalTable(User.Data);

				((NpgsqlConnection)db.OpenDbConnection()).ReloadTypes();

				var user  = new User() { Id = 1, Type = UserTypeEnum.Organization };
				var users = db.GetTable<User>().Where(x => x.InYourOrganization(user)).OrderBy(x => x.Id).Select(x => x.Id.ToString()).ToList();

				Assert.That(users, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(users[0], Is.EqualTo("1"));
					Assert.That(users[1], Is.EqualTo("3"));
				}

				user  = new User() { Id = 4, Type = UserTypeEnum.OrganizationUser, OrganizationId = 2 };
				users = db.GetTable<User>().Where(x => x.InYourOrganization(user)).OrderBy(x => x.Id).Select(x => x.Id.ToString()).ToList();

				Assert.That(users, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(users[0], Is.EqualTo("2"));
					Assert.That(users[1], Is.EqualTo("4"));
				}
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
