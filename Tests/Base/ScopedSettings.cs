using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data.DbCommandProcessor;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Linq;
using System;
using Tests.Model;

namespace Tests
{
	public class FirebirdQuoteMode : IDisposable
	{
		private readonly FirebirdIdentifierQuoteMode _oldMode;

		public FirebirdQuoteMode(FirebirdIdentifierQuoteMode mode)
		{
			_oldMode = FirebirdConfiguration.IdentifierQuoteMode;
			FirebirdConfiguration.IdentifierQuoteMode = mode;
		}

		void IDisposable.Dispose()
		{
			FirebirdConfiguration.IdentifierQuoteMode = _oldMode;
		}
	}

	public class CustomCommandProcessor : IDisposable
	{
		private readonly IDbCommandProcessor? _original = DbCommandProcessorExtensions.Instance;
		public CustomCommandProcessor(IDbCommandProcessor? processor)
		{
			DbCommandProcessorExtensions.Instance = processor;
		}

		public void Dispose()
		{
			DbCommandProcessorExtensions.Instance = _original;
		}
	}

	public class OptimizeForSequentialAccess : IDisposable
	{
		private readonly bool _original = Configuration.OptimizeForSequentialAccess;
		public OptimizeForSequentialAccess(bool enable)
		{
			Configuration.OptimizeForSequentialAccess = enable;
		}

		public void Dispose()
		{
			Configuration.OptimizeForSequentialAccess = _original;
		}
	}

	public class WithoutComparisonNullCheck : IDisposable
	{
		public WithoutComparisonNullCheck()
		{
			Configuration.Linq.CompareNullsAsValues = false;
		}

		public void Dispose()
		{
			Configuration.Linq.CompareNullsAsValues = true;
			Query.ClearCaches();
		}
	}

	public class DisableBaseline : IDisposable
	{
		private readonly CustomTestContext _ctx;
		private readonly bool _oldState;

		public DisableBaseline(string reason, bool disable = true)
		{
			_ctx = CustomTestContext.Get();
			_oldState = _ctx.Get<bool>(CustomTestContext.BASELINE_DISABLED);
			_ctx.Set(CustomTestContext.BASELINE_DISABLED, disable);
		}

		public void Dispose()
		{
			_ctx.Set(CustomTestContext.BASELINE_DISABLED, _oldState);
		}
	}

	public class DisableQueryCache : IDisposable
	{
		private readonly bool _oldValue = Configuration.Linq.DisableQueryCache;

		public DisableQueryCache(bool value = true)
		{
			Configuration.Linq.DisableQueryCache = value;
		}

		public void Dispose()
		{
			Configuration.Linq.DisableQueryCache = _oldValue;
		}
	}

	public class WithoutJoinOptimization : IDisposable
	{
		public WithoutJoinOptimization(bool opimizerSwitch = false)
		{
			Configuration.Linq.OptimizeJoins = opimizerSwitch;
			Query.ClearCaches();
		}

		public void Dispose()
		{
			Configuration.Linq.OptimizeJoins = true;
		}
	}

	public class DeletePerson : IDisposable
	{
		readonly IDataContext _db;

		public DeletePerson(IDataContext db)
		{
			_db = db;
			Delete(_db);
		}

		public void Dispose()
		{
			Delete(_db);
		}

		readonly Func<IDataContext,int> Delete =
			CompiledQuery.Compile<IDataContext, int>(db => db.GetTable<Person>().Delete(_ => _.ID > TestBase.MaxPersonID));
	}

	public class GenerateFinalAliases : IDisposable
	{
		private readonly bool _oldValue = Configuration.Sql.GenerateFinalAliases;

		public GenerateFinalAliases(bool enable)
		{
			Configuration.Sql.GenerateFinalAliases = enable;
		}

		public void Dispose()
		{
			Configuration.Sql.GenerateFinalAliases = _oldValue;
		}
	}

	public class SerializeAssemblyQualifiedName : IDisposable
	{
		private readonly bool _oldValue = Configuration.LinqService.SerializeAssemblyQualifiedName;

		public SerializeAssemblyQualifiedName(bool enable)
		{
			Configuration.LinqService.SerializeAssemblyQualifiedName = enable;
		}

		public void Dispose()
		{
			Configuration.LinqService.SerializeAssemblyQualifiedName = _oldValue;
		}
	}

	public class DisableLogging : IDisposable
	{
		private readonly CustomTestContext _ctx;
		private readonly bool _oldState;

		public DisableLogging()
		{
			_ctx = CustomTestContext.Get();
			_oldState = _ctx.Get<bool>(CustomTestContext.TRACE_DISABLED);
			_ctx.Set(CustomTestContext.TRACE_DISABLED, true);
		}

		public void Dispose()
		{
			_ctx.Set(CustomTestContext.TRACE_DISABLED, _oldState);
		}
	}

	public class GuardGrouping : IDisposable
	{
		private readonly bool _oldValue = Configuration.Linq.GuardGrouping;

		public GuardGrouping(bool enable)
		{
			Configuration.Linq.GuardGrouping = enable;
		}

		public void Dispose()
		{
			Configuration.Linq.GuardGrouping = _oldValue;
		}
	}

	public class ParameterizeTakeSkip : IDisposable
	{
		private readonly bool _oldValue = Configuration.Linq.ParameterizeTakeSkip;

		public ParameterizeTakeSkip(bool enable)
		{
			Configuration.Linq.ParameterizeTakeSkip = enable;
		}

		public void Dispose()
		{
			Configuration.Linq.ParameterizeTakeSkip = _oldValue;
		}
	}

	public class PreloadGroups : IDisposable
	{
		private readonly bool _oldValue = Configuration.Linq.PreloadGroups;

		public PreloadGroups(bool enable)
		{
			Configuration.Linq.PreloadGroups = enable;
		}

		public void Dispose()
		{
			Configuration.Linq.PreloadGroups = _oldValue;
		}
	}

	public class GenerateExpressionTest : IDisposable
	{
		private readonly bool _oldValue = Configuration.Linq.GenerateExpressionTest;

		public GenerateExpressionTest(bool enable)
		{
			Configuration.Linq.GenerateExpressionTest = enable;
		}

		public void Dispose()
		{
			Configuration.Linq.GenerateExpressionTest = _oldValue;
		}
	}

	public class DoNotClearOrderBys : IDisposable
	{
		private readonly bool _oldValue = Configuration.Linq.DoNotClearOrderBys;

		public DoNotClearOrderBys(bool enable)
		{
			Configuration.Linq.DoNotClearOrderBys = enable;
		}

		public void Dispose()
		{
			Configuration.Linq.DoNotClearOrderBys = _oldValue;
		}
	}

	public class OracleAlternativeBulkCopyMode : IDisposable
	{
		private readonly AlternativeBulkCopy _oldValue = OracleTools.UseAlternativeBulkCopy;

		public OracleAlternativeBulkCopyMode(AlternativeBulkCopy mode)
		{
			OracleTools.UseAlternativeBulkCopy = mode;
		}

		void IDisposable.Dispose()
		{
			OracleTools.UseAlternativeBulkCopy = _oldValue;
		}
	}
}
