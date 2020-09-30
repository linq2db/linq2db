using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	public interface IReadOnlyParameterValues
	{
		bool TryGetValue(SqlParameter parameter, [NotNullWhen(true)] out SqlParameterValue? value);
	}

	public class SqlParameterValues : IReadOnlyParameterValues
	{
		public static IReadOnlyParameterValues Empty = new SqlParameterValues();

		private Dictionary<SqlParameter, SqlParameterValue>? _values;

		public void AddValue(SqlParameter parameter, object? value, DbDataType dbDataType)
		{
			_values ??= new Dictionary<SqlParameter, SqlParameterValue>();
			_values.Remove(parameter);
			_values.Add(parameter, new SqlParameterValue(value, dbDataType));
		}

		public void SetValue(SqlParameter parameter, object? value)
		{
			_values ??= new Dictionary<SqlParameter, SqlParameterValue>();
			if (!_values.TryGetValue(parameter, out var parameterValue))
			{
				parameterValue = new SqlParameterValue(value, parameter.Type);
				_values.Add(parameter, parameterValue);
			}
			else
			{
				_values.Remove(parameter);
				_values.Add(parameter, new SqlParameterValue(value, parameterValue.DbDataType));
			}
		}

		public bool TryGetValue(SqlParameter parameter, [NotNullWhen(true)] out SqlParameterValue? value)
		{
			if (_values == null)
			{
				value = null;
				return false;

			}			
			return _values.TryGetValue(parameter, out value);
		}
	}
}
