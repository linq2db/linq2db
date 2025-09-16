using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	/// Детектор «транзиентности» исключений YDB без жёсткой зависимости от Ydb.Sdk.
	/// Работает по полям YdbException: bool IsTransient, enum Code.
	/// </summary>
	public static class YdbTransientExceptionDetector
	{
		private const string YdbExceptionFullName = "Ydb.Sdk.Ado.YdbException";

		/// <summary> Есть ли внутри/рядом YDB-исключение. </summary>
		public static bool TryGetYdbException(Exception ex, [NotNullWhen(true)] out Exception? ydbEx)
		{
			// YdbException всегда верхнеуровневое в ADO-клиенте, но на всякий случай «пройдёмся вниз».
			for (var e = ex; e != null; e = e.InnerException!)
			{
				if (e.GetType().FullName == YdbExceptionFullName)
				{
					ydbEx = e;
					return true;
				}
			}

			ydbEx = null;
			return false;
		}

		/// <summary> Прочитать YDB Code (enum) как строку имени, и IsTransient. </summary>
		public static bool TryGetCodeAndTransient(Exception ydbEx, out string? codeName, out bool isTransient)
		{
			var t = ydbEx.GetType();

			// bool IsTransient { get; }
			var isTransientProp = t.GetProperty("IsTransient", BindingFlags.Public | BindingFlags.Instance);
			isTransient = isTransientProp is not null && isTransientProp.GetValue(ydbEx) is bool b && b;

			// StatusCode Code { get; }
			var codeProp = t.GetProperty("Code", BindingFlags.Public | BindingFlags.Instance);
			var codeVal  = codeProp?.GetValue(ydbEx);
			codeName = Convert.ToString(codeVal, CultureInfo.InvariantCulture);
			return codeProp != null;
		}

		/// <summary>
		/// Минимальный детект «стоит ли вообще пытаться ретраить» для стратегии ретраев.
		/// Логика близка sdk: транзиентные статусы и сервисные таймауты.
		/// </summary>
		public static bool ShouldRetryOn(Exception ex, bool enableRetryIdempotence)
		{
			if (TryGetYdbException(ex, out var ydbEx))
			{
				_ = TryGetCodeAndTransient(ydbEx, out var code, out var isTransient);

				// Если idempotence выключен — ориентируемся только на IsTransient
				if (!enableRetryIdempotence)
					return isTransient;

				// При idempotence=true добавим набор кодов, которые sdk ретраит со своей схемой задержек.
				// (Используем имена enum-элементов, чтобы не тянуть сам enum из сборки.)
				return isTransient || code is
					"BadSession" or "SessionBusy" or
					"Aborted" or "Undetermined" or
					"Unavailable" or "ClientTransportUnknown" or "ClientTransportUnavailable" or
					"Overloaded" or "ClientTransportResourceExhausted";
			}

			// Плюс общие сетевые/временные случаи.
			return ex is TimeoutException;
		}
	}
}
