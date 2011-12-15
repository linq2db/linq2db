using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using LinqToDB.Data;
using LinqToDB.Data.DataProvider;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.Reflection;
using LinqToDB.Reflection.Extension;

namespace LinqToDB.DataAccess
{
	public abstract class SqlQueryBase : DataAccessorBase
	{
		#region Constructors

		protected SqlQueryBase()
		{
		}

		protected SqlQueryBase(DbManager dbManager)
			: base(dbManager)
		{
		}

		protected SqlQueryBase(DbManager dbManager, bool dispose)
			: base(dbManager, dispose)
		{
		}

		#endregion

		#region Protected Members

		protected virtual MemberMapper[] GetFieldList(ObjectMapper om)
		{
			var list = new List<MemberMapper>(om.Count);

			foreach (MemberMapper mm in om)
				if (mm.MapMemberInfo.SqlIgnore == false)
					list.Add(mm);

			return list.ToArray();
		}

		protected virtual MemberMapper[] GetNonKeyFieldList(ObjectMapper om)
		{
			var typeExt = TypeExtension.GetTypeExtension(om.TypeAccessor.Type, Extensions);
			var list    = new List<MemberMapper>();

			foreach (MemberMapper mm in om)
			{
				if (mm.MapMemberInfo.SqlIgnore)
					continue;

				var ma = mm.MapMemberInfo.MemberAccessor;

				bool isSet;
				MappingSchema.MetadataProvider.GetPrimaryKeyOrder(om.TypeAccessor.Type, typeExt, ma, out isSet);

				if (!isSet)
					list.Add(mm);
			}

			return list.ToArray();
		}

		struct MemberOrder
		{
			public MemberOrder(MemberMapper memberMapper, int order)
			{
				MemberMapper = memberMapper;
				Order        = order;
			}

			public readonly MemberMapper MemberMapper;
			public readonly int          Order;
		}

		private static readonly Hashtable _keyList = new Hashtable();

		protected internal virtual MemberMapper[] GetKeyFieldList(DbManager db, Type type)
		{
			var key    = type.FullName + "$" + db.DataProvider.UniqueName;
			var mmList = (MemberMapper[])_keyList[key];

			if (mmList == null)
			{
				var typeExt = TypeExtension.GetTypeExtension(type, Extensions);
				var list    = new List<MemberOrder>();

				foreach (MemberMapper mm in db.MappingSchema.GetObjectMapper(type))
				{
					if (mm.MapMemberInfo.SqlIgnore)
						continue;

					var ma = mm.MapMemberInfo.MemberAccessor;

					if (ma.Type.IsScalar())
					{
						bool isSet;
						var order = MappingSchema.MetadataProvider.GetPrimaryKeyOrder(type, typeExt, ma, out isSet);

						if (isSet)
							list.Add(new MemberOrder(mm, order));
					}
				}

				list.Sort((x, y) => x.Order - y.Order);

				_keyList[key] = mmList = new MemberMapper[list.Count];

				for (var i = 0; i < list.Count; i++)
					mmList[i] = list[i].MemberMapper;
			}

			return mmList;
		}

		protected virtual void AddWherePK(DbManager db, SqlQueryInfo query, StringBuilder sb, int nParameter)
		{
			sb.Append("WHERE\n");

			var memberMappers = GetKeyFieldList(db, query.ObjectType);

			if (memberMappers.Length == 0)
				throw new DataAccessException(
						string.Format("No primary key field(s) in the type '{0}'.", query.ObjectType.FullName));

			foreach (var mm in memberMappers)
			{
				var p = query.AddParameter(
					db.DataProvider.Convert(mm.Name + "_W", ConvertType.NameToQueryParameter).ToString(),
					mm.Name);

				sb.AppendFormat("\t{0} = ", db.DataProvider.Convert(p.FieldName, ConvertType.NameToQueryField));

				if (nParameter < 0)
					sb.AppendFormat("{0} AND\n", p.ParameterName);
				else
					sb.AppendFormat("{{{0}}} AND\n", nParameter++);
			}

			sb.Remove(sb.Length - 5, 5);
		}

		// NOTE changed to virtual
		protected virtual void AppendTableName(StringBuilder sb, DbManager db, Type type)
		{
			var database = GetDatabaseName(type);
			var owner    = GetOwnerName   (type);
			var name     = GetTableName   (type);

			db.DataProvider.CreateSqlProvider().BuildTableName(sb,
				database == null ? null : db.DataProvider.Convert(database, ConvertType.NameToDatabase).  ToString(),
				owner    == null ? null : db.DataProvider.Convert(owner,    ConvertType.NameToOwner).     ToString(),
				name     == null ? null : db.DataProvider.Convert(name,     ConvertType.NameToQueryTable).ToString());

			sb.AppendLine();
		}

		protected SqlQueryInfo CreateInsertSqlText(DbManager db, Type type, int nParameter)
		{
			var typeExt = TypeExtension.GetTypeExtension(type, Extensions);
			var om      = db.MappingSchema.GetObjectMapper(type);
			var list    = new List<MemberMapper>();
			var sb      = new StringBuilder();
			var query   = new SqlQueryInfo(om);
			var mp      = MappingSchema.MetadataProvider;

			sb.Append("INSERT INTO ");
			AppendTableName(sb, db, type);
			sb.Append(" (\n");

			foreach (var mm in GetFieldList(om))
			{
				// IT: This works incorrectly for complex mappers.
				//
				// [2009-03-24] ili: use mm.MemberAccessor instead of mm.ComplexMemberAccessor
				// as in CreateUpdateSqlText
				//

				bool isSet;
				var nonUpdatableAttribute = mp.GetNonUpdatableAttribute(type, typeExt, mm.MapMemberInfo.MemberAccessor, out isSet);

				if (nonUpdatableAttribute == null || !isSet || nonUpdatableAttribute.OnInsert == false)
				{
					sb.AppendFormat("\t{0},\n",
						db.DataProvider.Convert(mm.Name, ConvertType.NameToQueryField));
					list.Add(mm);
				}
			}

			sb.Remove(sb.Length - 2, 1);

			sb.Append(") VALUES (\n");

			foreach (var mm in list)
			{
				var p = query.AddParameter(
					db.DataProvider.Convert(mm.Name + "_P", ConvertType.NameToQueryParameter).ToString(),
					mm.Name);

				if (nParameter < 0)
					sb.AppendFormat("\t{0},\n", p.ParameterName);
					//sb.AppendFormat("\t{0},\n", db.DataProvider.Convert(p.ParameterName, ConvertType.NameToQueryParameter));
				else
					sb.AppendFormat("\t{{{0}}},\n", nParameter++);
			}

			sb.Remove(sb.Length - 2, 1);

			sb.Append(")");

			query.QueryText = sb.ToString();

			return query;
		}

		protected SqlQueryInfo CreateUpdateSqlText(DbManager db, Type type, int nParameter)
		{
			var typeExt = TypeExtension.GetTypeExtension(type, Extensions);
			var om      = db.MappingSchema.GetObjectMapper(type);
			var sb      = new StringBuilder();
			var query   = new SqlQueryInfo(om);
			var mp      = MappingSchema.MetadataProvider;

			sb.Append("UPDATE\n\t");
			AppendTableName(sb, db, type);
			sb.Append("\nSET\n");

			var fields    = GetFieldList(om);
			var hasFields = false;

			foreach (var mm in fields)
			{
				bool isSet;

				var nonUpdatableAttribute = mp.GetNonUpdatableAttribute(type, typeExt, mm.MapMemberInfo.MemberAccessor, out isSet);

				if (nonUpdatableAttribute != null && isSet && nonUpdatableAttribute.OnUpdate)
					continue;

				mp.GetPrimaryKeyOrder(type, typeExt, mm.MapMemberInfo.MemberAccessor, out isSet);

				if (isSet)
					continue;

				hasFields = true;

				var p = query.AddParameter(
					db.DataProvider.Convert(mm.Name + "_P", ConvertType.NameToQueryParameter).ToString(),
					mm.Name);

				sb.AppendFormat("\t{0} = ", db.DataProvider.Convert(p.FieldName, ConvertType.NameToQueryField));

				if (nParameter < 0)
					sb.AppendFormat("{0},\n", p.ParameterName);
				else
					sb.AppendFormat("\t{{{0}}},\n", nParameter++);
			}

			if (!hasFields)
				throw new DataAccessException(
						string.Format("There are no fields to update in the type '{0}'.", query.ObjectType.FullName));

			sb.Remove(sb.Length - 2, 1);

			AddWherePK(db, query, sb, nParameter);

			query.QueryText = sb.ToString();

			return query;
		}

		protected SqlQueryInfo CreateDeleteSqlText(DbManager db, Type type, int nParameter)
		{
			var om    = db.MappingSchema.GetObjectMapper(type);
			var sb    = new StringBuilder();
			var query = new SqlQueryInfo(om);

			sb.Append("DELETE FROM\n\t");
			AppendTableName(sb, db, type);
			sb.AppendLine();

			AddWherePK(db, query, sb, nParameter);

			query.QueryText = sb.ToString();

			return query;
		}

		protected virtual SqlQueryInfo CreateSqlText(DbManager db, Type type, string actionName)
		{
			switch (actionName)
			{
				case "Insert":      return CreateInsertSqlText     (db, type, -1);
				case "InsertBatch": return CreateInsertSqlText     (db, type,  0);
				case "Update":      return CreateUpdateSqlText     (db, type, -1);
				case "UpdateBatch": return CreateUpdateSqlText     (db, type,  0);
				case "Delete":      return CreateDeleteSqlText     (db, type, -1);
				case "DeleteBatch": return CreateDeleteSqlText     (db, type,  0);
				default:
					throw new DataAccessException(
						string.Format("Unknown action '{0}'.", actionName));
			}
		}

		private static readonly Hashtable _actionSqlQueryInfo = new Hashtable();

		public virtual SqlQueryInfo GetSqlQueryInfo(DbManager db, Type type, string actionName)
		{
			var key   = type.FullName + "$" + actionName + "$" + db.DataProvider.UniqueName + "$" + GetTableName(type);
			var query = (SqlQueryInfo)_actionSqlQueryInfo[key];

			if (query == null)
			{
				query = CreateSqlText(db, type, actionName);
				_actionSqlQueryInfo[key] = query;
			}

			return query;
		}

		#endregion
	}
}
