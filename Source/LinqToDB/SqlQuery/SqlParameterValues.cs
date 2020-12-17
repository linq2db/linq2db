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

		private Dictionary<SqlParameter, SqlParameterValue>? _valuesByParameter;
		private Dictionary<int, SqlParameterValue>?          _valuesByAccessor;

		public void AddValue(SqlParameter parameter, object? value, DbDataType dbDataType)
		{
			_valuesByParameter ??= new Dictionary<SqlParameter, SqlParameterValue>();

			var parameterValue = new SqlParameterValue(value, dbDataType);

			_valuesByParameter.Remove(parameter);
			_valuesByParameter.Add(parameter, parameterValue);

			if (parameter.AccessorId != null)
			{
				_valuesByAccessor  ??= new Dictionary<int, SqlParameterValue>();
				_valuesByAccessor.Remove(parameter.AccessorId.Value);
				_valuesByAccessor.Add(parameter.AccessorId.Value, parameterValue);
			}
		}

		public void SetValue(SqlParameter parameter, object? value)
		{
			_valuesByParameter ??= new Dictionary<SqlParameter, SqlParameterValue>();
			if (!_valuesByParameter.TryGetValue(parameter, out var parameterValue))
			{
				parameterValue = new SqlParameterValue(value, parameter.Type);
				_valuesByParameter.Add(parameter, parameterValue);
			}
			else
			{
				_valuesByParameter.Remove(parameter);
				_valuesByParameter.Add(parameter, new SqlParameterValue(value, parameterValue.DbDataType));
			}

			if (parameter.AccessorId != null)
			{
				_valuesByAccessor ??= new Dictionary<int, SqlParameterValue>();
				if (!_valuesByAccessor.TryGetValue(parameter.AccessorId.Value, out parameterValue))
				{
					parameterValue = new SqlParameterValue(value, parameter.Type);
					_valuesByAccessor.Add(parameter.AccessorId.Value, parameterValue);
				}
				else
				{
					_valuesByAccessor.Remove(parameter.AccessorId.Value);
					_valuesByAccessor.Add(parameter.AccessorId.Value, new SqlParameterValue(value, parameterValue.DbDataType));
				}
			}
		}

		public bool TryGetValue(SqlParameter parameter, [NotNullWhen(true)] out SqlParameterValue? value)
		{
			value = null;
			if (_valuesByParameter?.TryGetValue(parameter, out value) == false 
			    && parameter.AccessorId != null && _valuesByAccessor?.TryGetValue(parameter.AccessorId.Value, out value) == false)
			{
				return false;
			}			

			return value != null;
		}
	}
}
