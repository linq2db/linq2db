using System;
using System.Collections;
using System.Data;

using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Properties;
using LinqToDB.Reflection.Extension;

namespace LinqToDB.DataAccess
{
	public abstract class DataAccessorBase
	{
		#region Constructors

		[System.Diagnostics.DebuggerStepThrough]
		protected DataAccessorBase()
		{
		}

		[System.Diagnostics.DebuggerStepThrough]
		protected DataAccessorBase(DbManager dbManager)
		{
			SetDbManager(dbManager, false);
		}

		[System.Diagnostics.DebuggerStepThrough]
		protected DataAccessorBase(DbManager dbManager, bool dispose)
		{
			SetDbManager(dbManager, dispose);
		}

		#endregion

		#region Public Members

		[System.Diagnostics.DebuggerStepThrough]
		public virtual DbManager GetDbManager()
		{
			return _dbManager ?? CreateDbManager();
		}

		protected virtual DbManager CreateDbManager()
		{
			return new DbManager();
		}

		public virtual void BeginTransaction()
		{
			if (_dbManager == null)
				throw new InvalidOperationException(Resources.DataAccessorBase_NoDbManager);

			_dbManager.BeginTransaction();
		}

		public virtual void BeginTransaction(IsolationLevel il)
		{
			if (_dbManager == null)
				throw new InvalidOperationException(Resources.DataAccessorBase_NoDbManager);

			_dbManager.BeginTransaction(il);
		}

		public virtual void CommitTransaction()
		{
			if (_dbManager == null)
				throw new InvalidOperationException(Resources.DataAccessorBase_NoDbManager);

			_dbManager.CommitTransaction();
		}

		public virtual void RollbackTransaction()
		{
			if (_dbManager == null)
				throw new InvalidOperationException(Resources.DataAccessorBase_NoDbManager);

			_dbManager.RollbackTransaction();
		}

		private ExtensionList _extensions;
		public  ExtensionList  Extensions
		{
			get { return _extensions ?? (_extensions = MappingSchema.Extensions); }
			set { _extensions = value; }
		}

		private        bool _disposeDbManager = true;
		public virtual bool  DisposeDbManager
		{
			get { return _disposeDbManager;  }
			set { _disposeDbManager = value; }
		}

		private MappingSchemaOld _mappingSchema;
		public  MappingSchemaOld  MappingSchema
		{
			get { return _mappingSchema ?? (_mappingSchema = _dbManager != null? _dbManager.MappingSchema: Map.DefaultSchema); }
			set { _mappingSchema = value; }
		}

		#endregion

		#region Protected Members

		private   DbManager _dbManager;
		protected DbManager  DbManager
		{
			get { return _dbManager; }
		}

		protected internal void SetDbManager(DbManager dbManager, bool dispose)
		{
			_dbManager        = dbManager;
			_disposeDbManager = dispose;
		}

		protected virtual string GetDefaultSpName(string typeName, string actionName)
		{
			return typeName == null?
				actionName:
				string.Format("{0}_{1}", typeName, actionName);
		}

		protected virtual string GetDatabaseName(Type type)
		{
			bool isSet;
			return MappingSchema.MetadataProvider.GetDatabaseName(type, Extensions, out isSet);
		}

		protected virtual string GetOwnerName(Type type)
		{
			bool isSet;
			return MappingSchema.MetadataProvider.GetOwnerName(type, Extensions, out isSet);
		}

		protected virtual string GetTableName(Type type)
		{
			bool isSet;
			return MappingSchema.MetadataProvider.GetTableName(type, Extensions, out isSet);
		}

		protected virtual void Dispose(DbManager dbManager)
		{
			if (dbManager != null && DisposeDbManager)
				dbManager.Dispose();
		}

		#endregion
	}
}
