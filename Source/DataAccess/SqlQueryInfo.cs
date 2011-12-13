using System;
using System.Collections.Generic;
using System.Data;

using LinqToDB.Data;
using LinqToDB.Mapping;

namespace LinqToDB.DataAccess
{
	public class SqlQueryInfo
	{
		//NOTE Added empty constructor
		public SqlQueryInfo()
		{
		}

		public SqlQueryInfo(ObjectMapper objectMapper)
		{
			ObjectMapper = objectMapper;
		}

		public string       QueryText    { get; set; }
		public ObjectMapper ObjectMapper { get; private set; }

		public Type ObjectType
		{
			get { return ObjectMapper.TypeAccessor.OriginalType; }
		}

		//NOTE Changed from private to protected
		protected readonly List<SqlQueryParameterInfo> Parameters = new List<SqlQueryParameterInfo>();

		//NOTE Changed to virtual
		public virtual SqlQueryParameterInfo AddParameter(string parameterName, string fieldName)
		{
			var parameter = new SqlQueryParameterInfo { ParameterName = parameterName, FieldName = fieldName };

			parameter.SetMemberMapper(ObjectMapper);

			Parameters.Add(parameter);

			return parameter;
		}

		public IDbDataParameter[] GetParameters(DbManager db, object[] key)
		{
			if (Parameters.Count != key.Length)
				throw new DataAccessException("Parameter list does match key list.");

			var parameters = new IDbDataParameter[Parameters.Count];

			for (var i = 0; i < Parameters.Count; i++)
			{
				var info = Parameters[i];

				parameters[i] = db.Parameter(info.ParameterName, key[i]);
			}

			return parameters;
		}

		public IDbDataParameter[] GetParameters(DbManager db, object obj)
		{
			var parameters = new IDbDataParameter[Parameters.Count];

			for (var i = 0; i < Parameters.Count; i++)
			{
				var info = Parameters[i];

				//parameters[i] = db.Parameter(info.ParameterName, info.MemberMapper.GetValue(obj));

				var mmi = info.MemberMapper.MapMemberInfo;
				var val = info.MemberMapper.GetValue(obj);

				if (val == null && mmi.Nullable/* && mmi.NullValue == null*/)
				{
					//replace value with DbNull
					val = DBNull.Value;
				}

				if (mmi.IsDbTypeSet)
				{
					parameters[i] = mmi.IsDbSizeSet 
						? db.Parameter(info.ParameterName, val, info.MemberMapper.DbType, mmi.DbSize) 
						: db.Parameter(info.ParameterName, val, info.MemberMapper.DbType);
				}
				else
				{
					parameters[i] = db.Parameter(info.ParameterName, val);
				}
			}

			return parameters;
		}

		public MemberMapper[] GetMemberMappers()
		{
			var members = new MemberMapper[Parameters.Count];

			for (var i = 0; i < Parameters.Count; i++)
				members[i] = Parameters[i].MemberMapper;

			return members;
		}
	}
}
