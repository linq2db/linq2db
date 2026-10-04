using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	/// <summary>
	/// Extension points a <see cref="DataConnection"/> subclass owns have to keep working when the combined-command
	/// engine is on. The combined path builds its own plan from the query's statement instead of going through
	/// <c>DataConnection.QueryRunner.GetCommand</c>, so everything <c>GetCommand</c> does on the way to the render is
	/// either reproduced there or explicitly opted out of - silently skipping it is the failure this fixture pins.
	/// </summary>
	[TestFixture]
	public class CombinedCommandsExtensibilityTests : TestBase
	{
		[Table]
		sealed class CeParent
		{
			[PrimaryKey] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(CeChild.ParentId))]
			public List<CeChild> Children { get; set; } = null!;
		}

		[Table]
		sealed class CeChild
		{
			[PrimaryKey] public int Id       { get; set; }
			[Column    ] public int ParentId { get; set; }
		}

		sealed class ProcessQueryCountingConnection : DataConnection
		{
			public ProcessQueryCountingConnection(DataOptions options) : base(options)
			{
			}

			public int ProcessQueryCount { get; set; }

			protected override SqlStatement ProcessQuery(SqlStatement statement, EvaluationContext context)
			{
				ProcessQueryCount++;
				return base.ProcessQuery(statement, context);
			}
		}

		sealed class SelectCounter : CommandInterceptor
		{
			public int Count { get; set; }

			public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
			{
				var sql = command.CommandText;

				if (sql.Contains("SELECT", StringComparison.OrdinalIgnoreCase)
					&& !sql.Contains("CREATE", StringComparison.OrdinalIgnoreCase)
					&& !sql.Contains("DROP",   StringComparison.OrdinalIgnoreCase)
					&& !sql.Contains("INSERT", StringComparison.OrdinalIgnoreCase))
				{
					Count++;
				}

				return command;
			}
		}

		// Anti-trivial guard for the test below: proves the option really does collapse this eager load into one
		// command on this provider, so a later "the override still saw everything" result cannot be satisfied by a
		// combined path that never engaged in the first place.
		[Test]
		public void CombinedEagerLoad_CollapsesToOneCommand([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = new DataConnection(new DataOptions()
				.UseConfiguration(context)
				.UseCombinedCommands(true));

			var counter = new SelectCounter();
			db.AddInterceptor(counter);

			using var parents  = db.CreateLocalTable<CeParent>();
			using var children = db.CreateLocalTable<CeChild>();

			Seed(db);
			counter.Count = 0;

			parents.LoadWith(p => p.Children).ToList();

			counter.Count.ShouldBe(1);
		}

		// ProcessQuery is a shipped protected virtual extension point whose only caller is GetCommand. The combined
		// path renders from QueryInfo.Statement and each child's GetCombinableStatement() directly, so without a guard
		// a subclass's rewrite silently stops applying to the main query and every combinable child while still firing
		// for the non-combinable ones - a per-harvester split in behaviour with no diagnostic.
		[Test]
		public void ProcessQueryOverride_SeesEveryStatement_WhenCombining([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var sequential = RunAndCountProcessQuery(context, combined: false);
			var combined   = RunAndCountProcessQuery(context, combined: true);

			sequential.ShouldBeGreaterThan(1);  // the eager load really is more than one statement
			combined.ShouldBe(sequential);
		}

		static int RunAndCountProcessQuery(string context, bool combined)
		{
			using var db = new ProcessQueryCountingConnection(new DataOptions()
				.UseConfiguration(context)
				.UseCombinedCommands(combined));

			using var parents  = db.CreateLocalTable<CeParent>();
			using var children = db.CreateLocalTable<CeChild>();

			Seed(db);
			db.ProcessQueryCount = 0;

			var loaded = parents.LoadWith(p => p.Children).ToList();

			loaded.ShouldHaveSingleItem();
			loaded[0].Children.Count.ShouldBe(2);

			return db.ProcessQueryCount;
		}

		static void Seed(DataConnection db)
		{
			db.Insert(new CeParent { Id = 1 });
			db.Insert(new CeChild { Id = 1, ParentId = 1 });
			db.Insert(new CeChild { Id = 2, ParentId = 1 });
		}
	}
}
