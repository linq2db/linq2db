using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.Data
{
	using System.Data;

	/// <summary>
	/// Tracing information for the <see cref="DataConnection"/> events.
	/// </summary>
	public class TraceInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TraceInfo"/> class.
		/// </summary>
		/// <param name="traceInfoStep">Trace execution step.</param>
		public TraceInfo(TraceInfoStep traceInfoStep)
		{
			TraceInfoStep = traceInfoStep;
		}

		/// <summary>
		/// Gets the tracing execution step, <see cref="TraceInfoStep"/>.
		/// </summary>
		public TraceInfoStep TraceInfoStep { get; private set; }

		/// <summary>
		/// Gets or sets the tracing detail level, <see cref="TraceLevel"/>.
		/// </summary>
		public TraceLevel TraceLevel { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="DataConnection"/> that produced the tracing event.
		/// </summary>
		public DataConnection DataConnection { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="IDbCommand"/> associated with the tracing event.
		/// </summary>
		public IDbCommand Command { get; set; }

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
		public Exception Exception { get; set; }

		/// <summary>
		/// Gets or sets the text of the command.
		/// </summary>
		public string CommandText { get; set; }

		/// <summary>
		/// Gets or sets the expression used by the results mapper.
		/// </summary>
		public Expression MapperExpression { get; set; }

		/// <summary>
		/// Gets or sets a flag indicating whether the command was executed asynchronously.
		/// </summary>
		public bool IsAsync { get; set; }

		/// <summary>
		/// Gets a flag indicating whether this step was executed before the operation.
		/// </summary>
		[Obsolete("Use TraceInfoStep instead.")]
		public bool BeforeExecute { get { return TraceInfoStep == TraceInfoStep.BeforeExecute; } }

		private string _sqlText;

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

					var sqlProvider = DataConnection.DataProvider.CreateSqlBuilder();
					var sb          = new StringBuilder();

					sb.Append("-- ").Append(DataConnection.ConfigurationString);

					if (DataConnection.ConfigurationString != DataConnection.DataProvider.Name)
						sb.Append(' ').Append(DataConnection.DataProvider.Name);

					if (DataConnection.DataProvider.Name != sqlProvider.Name)
						sb.Append(' ').Append(sqlProvider.Name);

					if (IsAsync)
						sb.Append(" (asynchronously)");

					sb.AppendLine();

					sqlProvider.PrintParameters(sb, Command.Parameters.Cast<IDbDataParameter>().ToArray());

					sb.AppendLine(Command.CommandText);

					while (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r')
						sb.Length--;

					sb.AppendLine();

					return _sqlText = sb.ToString();
				}

				return "";
			}
		}
	}
}
