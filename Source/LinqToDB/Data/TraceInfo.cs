using System;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common.Internal;

namespace LinqToDB.Data
{
	/// <summary>
	/// Tracing information for the <see cref="DataConnection"/> events.
	/// </summary>
	public class TraceInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TraceInfo"/> class.
		/// </summary>
		/// <param name="dataConnection"><see cref="DataConnection"/> instance, generated this trace.</param>
		/// <param name="traceInfoStep">Trace execution step.</param>
		/// <param name="operation">Operation associated with trace event.</param>
		/// <param name="isAsync">Flag indicating whether operation was executed asynchronously.</param>
		public TraceInfo(DataConnection dataConnection, TraceInfoStep traceInfoStep, TraceOperation operation, bool isAsync)
		{
			DataConnection = dataConnection;
			TraceInfoStep  = traceInfoStep;
			Operation      = operation;
			IsAsync        = isAsync;
		}

		/// <summary>
		/// Gets the tracing execution step, <see cref="TraceInfoStep"/>.
		/// </summary>
		public TraceInfoStep TraceInfoStep { get; }

		/// <summary>
		/// Gets the operation, for which tracing event generated, <see cref="TraceOperation"/>.
		/// </summary>
		public TraceOperation Operation { get; }

		/// <summary>
		/// Gets or sets the tracing detail level, <see cref="TraceLevel"/>.
		/// </summary>
		public TraceLevel TraceLevel { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="DataConnection"/> that produced the tracing event.
		/// </summary>
		public DataConnection DataConnection { get; }

		/// <summary>
		/// Gets or sets the <see cref="DbCommand"/> associated with the tracing event.
		/// </summary>
		public DbCommand? Command { get; set; }

		/// <summary>
		/// Gets or sets the starting <see cref="DateTime"/> of the operation (UTC).
		/// </summary>
		public DateTime? StartTime { get; set; }

		/// <summary>
		/// Gets or sets the execution time for <see cref="TraceInfoStep.AfterExecute"/>,
		/// <see cref="TraceInfoStep.Completed"/>, and <see cref="TraceInfoStep.Error"/> steps.
		/// </summary>
		public TimeSpan? ExecutionTime { get; set; }

		/// <summary>
		/// Gets or sets the number of rows affected by the command
		/// or the number of rows produced by the <see cref="DataReader"/>.
		/// </summary>
		public int? RecordsAffected { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Exception"/> for <see cref="TraceInfoStep.Error"/> step.
		/// </summary>
		public Exception? Exception { get; set; }

		/// <summary>
		/// Gets or sets the text of the command.
		/// </summary>
		public string? CommandText { get; set; }

		/// <summary>
		/// Gets or sets the expression used by the results mapper.
		/// </summary>
		public Expression? MapperExpression { get; set; }

		/// <summary>
		/// Gets a flag indicating whether operation was executed asynchronously.
		/// </summary>
		public bool IsAsync { get; }

		private string? _sqlText;

		/// <summary>
		/// Gets the formatted SQL text of the command.
		/// </summary>
		public string SqlText
		{
			get
			{
				if (CommandText != null)
					return CommandText;

				if (Command != null)
				{
					if (_sqlText != null)
						return _sqlText;

					var sqlProvider = DataConnection.DataProvider.CreateSqlBuilder(DataConnection.MappingSchema, DataConnection.Options);
					using var sbv    = Pools.StringBuilder.Allocate();
					var sb           = sbv.Value;

					sb.Append("--");

					if (DataConnection.ConfigurationString != null)
						sb.Append(' ').Append(DataConnection.ConfigurationString);

					if (DataConnection.ConfigurationString != DataConnection.DataProvider.Name)
						sb.Append(' ').Append(DataConnection.DataProvider.Name);

					if (DataConnection.DataProvider.Name != sqlProvider.Name)
						sb.Append(' ').Append(sqlProvider.Name);

					if (IsAsync || DataConnection.Tag is not null)
						sb.Append(" (");

					if (IsAsync)
					{
						sb.Append("asynchronously");

						if (DataConnection.Tag is not null)
							sb.Append(", ");
					}

					if (DataConnection.Tag is not null)
						sb.Append(DataConnection.Tag);

					if (IsAsync || DataConnection.Tag is not null)
						sb.Append(')');

					sb.AppendLine();

					sqlProvider.PrintParameters(DataConnection, sb, Command.Parameters.Cast<DbParameter>());

					sb.AppendLine(Command.CommandText);

					while (sb[^1] is '\n' or '\r')
						sb.Length--;

					sb.AppendLine();

					return _sqlText = sb.ToString();
				}

				return "";
			}
		}
	}
}
