﻿using System;
using System.Globalization;
using System.Threading;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Mapping;

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

	public sealed class CultureRegion : IDisposable
	{
		private readonly CultureInfo _original;

		public CultureRegion(string culture)
		{
			_original = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
		}

		void IDisposable.Dispose()
		{
			Thread.CurrentThread.CurrentCulture = _original;
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
}
