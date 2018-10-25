using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.Data
{
	using System.Data;

	public class TraceInfo
	{
		public TraceInfo(TraceInfoStep traceInfoStep)
		{
			TraceInfoStep = traceInfoStep;
		}

		public TraceInfoStep  TraceInfoStep    { get; private set; }
		public TraceLevel     TraceLevel       { get; set; }
		public DataConnection DataConnection   { get; set; }
		public IDbCommand     Command          { get; set; }
		public DateTime?      StartTime        { get; set; }
		public TimeSpan?      ExecutionTime    { get; set; }
		public int?           RecordsAffected  { get; set; }
		public Exception      Exception        { get; set; }
		public string         CommandText      { get; set; }
		public Expression     MapperExpression { get; set; }
		public bool           IsAsync          { get; set; }

		[Obsolete("Use TraceInfoStep instead.")]
		public bool BeforeExecute              { get { return TraceInfoStep == TraceInfoStep.BeforeExecute; } }

		private string _sqlText;
		public  string  SqlText
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
