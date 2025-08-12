using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlParameterValues : IReadOnlyParameterValues
	{
		public static readonly IReadOnlyParameterValues Empty = new SqlParameterValues();

		private Dictionary<SqlParameter, SqlParameterValue>? _valuesByParameter;
		private Dictionary<int, SqlParameterValue>?          _valuesByAccessor;

		public void AddValue(SqlParameter parameter, object? providerValue, object? clientValue, in DbDataType dbDataType)
		{
			_valuesByParameter ??= new ();

			var parameterValue = new SqlParameterValue(providerValue, clientValue, dbDataType);

			_valuesByParameter.Remove(parameter);
			_valuesByParameter.Add(parameter, parameterValue);

			if (parameter.AccessorId != null)
			{
				_valuesByAccessor  ??= new ();
				_valuesByAccessor.Remove(parameter.AccessorId.Value);
				_valuesByAccessor.Add(parameter.AccessorId.Value, parameterValue);
			}
		}

		public void SetValue(SqlParameter parameter, object? providerValue, object? clientValue)
		{
			_valuesByParameter ??= new ();
			if (!_valuesByParameter.TryGetValue(parameter, out var parameterValue))
			{
				parameterValue = new SqlParameterValue(providerValue, clientValue, parameter.Type);
				_valuesByParameter.Add(parameter, parameterValue);
			}
			else
			{
				_valuesByParameter.Remove(parameter);
				_valuesByParameter.Add(parameter, new SqlParameterValue(providerValue, clientValue, parameterValue.DbDataType));
			}

			if (parameter.AccessorId != null)
			{
				_valuesByAccessor ??= new ();
				if (!_valuesByAccessor.TryGetValue(parameter.AccessorId.Value, out parameterValue))
				{
					parameterValue = new SqlParameterValue(providerValue, clientValue, parameter.Type);
					_valuesByAccessor.Add(parameter.AccessorId.Value, parameterValue);
				}
				else
				{
					_valuesByAccessor.Remove(parameter.AccessorId.Value);
					_valuesByAccessor.Add(parameter.AccessorId.Value, new SqlParameterValue(providerValue, clientValue, parameterValue.DbDataType));
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
