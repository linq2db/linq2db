﻿using LinqToDB;
using LinqToDB.Common;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using System;
using System.Globalization;
using System.Threading;
using Tests.Model;

namespace Tests
{
	public class RestoreBaseTables : IDisposable
	{
		private readonly IDataContext _db;

		public RestoreBaseTables(IDataContext db)
		{
			_db = db;
		}

		void IDisposable.Dispose()
		{
			using var _ = new DisableBaseline("isn't baseline query");

			_db.GetTable<Parent>().Delete(p => p.ParentID > 7);
			_db.GetTable<Child>().Delete(p => p.ParentID > 7 || p.ChildID > 77);

			_db.GetTable<Patient>().Delete(p => p.PersonID > 4 || p.PersonID < 1);
			_db.GetTable<Person>().Delete(p => p.ID > 4 || p.ID < 1);

			_db.GetTable<LinqDataTypes2>().Delete(p => p.ID > 12 || p.ID < 1);
			_db.GetTable<LinqDataTypes>()
				.Set(_ => _.BinaryValue, () => null)
				.Update();

			_db.GetTable<AllTypes>().Delete(p => p.ID > 2 || p.ID < 1);
		}

		[Table]
		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		public class AllTypes
		{
			[Column] public int ID { get; set; }
		}
		}

	public class InvariantCultureRegion : IDisposable
	{
		private readonly CultureInfo? _original;

		public InvariantCultureRegion()
		{
			if (!Thread.CurrentThread.CurrentCulture.Equals(CultureInfo.InvariantCulture))
			{
				_original = Thread.CurrentThread.CurrentCulture;
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			}
		}

		void IDisposable.Dispose()
		{
			if (_original != null)
				Thread.CurrentThread.CurrentCulture = _original;
		}
	}

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

	public class CompareNullsAsValuesOption : IDisposable
	{
		private readonly bool _original = Configuration.Linq.CompareNullsAsValues;

		public CompareNullsAsValuesOption(bool enable)
		{
			Configuration.Linq.CompareNullsAsValues = enable;
		}

		void IDisposable.Dispose()
		{
			Configuration.Linq.CompareNullsAsValues = _original;
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

	public class PreferApply : IDisposable
	{
		private readonly bool _oldValue = Configuration.Linq.PreferApply;

		public PreferApply(bool enable)
		{
			Configuration.Linq.PreferApply = enable;
		}

		public void Dispose()
		{
			Configuration.Linq.PreferApply = _oldValue;
		}
	}


}
