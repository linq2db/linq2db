using System;
using System.Collections.Generic;

using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Reflection.Extension;

namespace LinqToDB.DataAccess
{
	public class SqlQuery<T> : SqlQueryBase
	{
		#region Constructors

		public SqlQuery()
		{
		}

		public SqlQuery(DbManager dbManager)
			: base(dbManager)
		{
		}

		public SqlQuery(DbManager dbManager, bool dispose)
			: base(dbManager, dispose)
		{
		}

		public SqlQuery(ExtensionList extensions)
		{
			Extensions = extensions;
		}

		#endregion

		#region Overrides

		public SqlQueryInfo GetSqlQueryInfo(DbManager db, string actionName)
		{
			return base.GetSqlQueryInfo(db, typeof(T), actionName);
		}

		#endregion

		#region Insert

		public virtual int Insert(DbManager db, T obj)
		{
			var query = GetSqlQueryInfo(db, obj.GetType(), "Insert");

			return db
				.SetCommand(query.QueryText, query.GetParameters(db, obj))
				.ExecuteNonQuery();
		}

		public virtual int Insert(T obj)
		{
			var db = GetDbManager();

			try
			{
				return Insert(db, obj);
			}
			finally
			{
				if (DisposeDbManager)
					db.Dispose();
			}
		}

		public virtual int Insert(DbManager db, int maxBatchSize, IEnumerable<T> list)
		{
			var query = GetSqlQueryInfo(db, typeof(T), "InsertBatch");

			return db.DataProvider.InsertBatch(
				db,
				query.QueryText,
				list,
				query.GetMemberMappers(),
				maxBatchSize,
				obj => query.GetParameters(db, obj));
		}

		public virtual int Insert(int maxBatchSize, IEnumerable<T> list)
		{
			var db = GetDbManager();

			try
			{
				return Insert(db, maxBatchSize, list);
			}
			finally
			{
				if (DisposeDbManager)
					db.Dispose();
			}
		}

		public virtual int Insert(DbManager db, IEnumerable<T> list)
		{
			return Insert(db, int.MaxValue, list);
		}

		public virtual int Insert(IEnumerable<T> list)
		{
			return Insert(int.MaxValue, list);
		}

		#endregion

		#region Update

		public virtual int Update(DbManager db, T obj)
		{
			var query = GetSqlQueryInfo(db, obj.GetType(), "Update");

			return db
				.SetCommand(query.QueryText, query.GetParameters(db, obj))
				.ExecuteNonQuery();
		}

		public virtual int Update(T obj)
		{
			var db = GetDbManager();

			try
			{
				return Update(db, obj);
			}
			finally
			{
				if (DisposeDbManager)
					db.Dispose();
			}
		}

		public virtual int Update(DbManager db, int maxBatchSize, IEnumerable<T> list)
		{
			var query = GetSqlQueryInfo(db, typeof(T), "UpdateBatch");

			db.SetCommand(query.QueryText);

			return ExecuteForEach(
				db,
				list,
				query.GetMemberMappers(),
				maxBatchSize,
				obj => query.GetParameters(db, obj));
		}

		public virtual int Update(int maxBatchSize, IEnumerable<T> list)
		{
			var db = GetDbManager();

			try
			{
				return Update(db, maxBatchSize, list);
			}
			finally
			{
				if (DisposeDbManager)
					db.Dispose();
			}
		}

		public virtual int Update(DbManager db, IEnumerable<T> list)
		{
			return Update(db, int.MaxValue, list);
		}

		public virtual int Update(IEnumerable<T> list)
		{
			return Update(int.MaxValue, list);
		}

		#endregion

		#region DeleteByKey

		public virtual int DeleteByKey(DbManager db, params object[] key)
		{
			var query = GetSqlQueryInfo(db, typeof(T), "Delete");

			return db
				.SetCommand(query.QueryText, query.GetParameters(db, key))
				.ExecuteNonQuery();
		}

		public virtual int DeleteByKey(params object[] key)
		{
			var db = GetDbManager();

			try
			{
				return DeleteByKey(db, key);
			}
			finally
			{
				if (DisposeDbManager)
					db.Dispose();
			}
		}

		#endregion

		#region Delete

		public virtual int Delete(DbManager db, T obj)
		{
			var query = GetSqlQueryInfo(db, obj.GetType(), "Delete");

			return db
				.SetCommand(query.QueryText, query.GetParameters(db, obj))
				.ExecuteNonQuery();
		}

		public virtual int Delete(T obj)
		{
			var db = GetDbManager();

			try
			{
				return Delete(db, obj);
			}
			finally
			{
				if (DisposeDbManager)
					db.Dispose();
			}
		}

		public virtual int Delete(DbManager db, int maxBatchSize, IEnumerable<T> list)
		{
			var query = GetSqlQueryInfo(db, typeof(T), "DeleteBatch");

			db.SetCommand(query.QueryText);

			return ExecuteForEach(
				db,
				list,
				query.GetMemberMappers(),
				maxBatchSize,
				obj => query.GetParameters(db, obj));
		}

		public virtual int Delete(int maxBatchSize, IEnumerable<T> list)
		{
			var db = GetDbManager();

			try
			{
				return Delete(db, list);
			}
			finally
			{
				if (DisposeDbManager)
					db.Dispose();
			}
		}

		public virtual int Delete(DbManager db, IEnumerable<T> list)
		{
			return Delete(db, int.MaxValue, list);
		}

		public virtual int Delete(IEnumerable<T> list)
		{
			return Delete(int.MaxValue, list);
		}

		#endregion

		#region Helpers

		protected int ExecuteForEach(
			DbManager      db,
			IEnumerable<T> collection,
			MemberMapper[] members,
			int            maxBatchSize,
			DbManager.ParameterProvider<T> getParameters)
		{
			return db.ExecuteForEach(collection, members, maxBatchSize, getParameters);
		}

		#endregion
	}
}
