using System;
using System.Data;
using System.Data.OleDb;
using System.Text.RegularExpressions;

namespace LinqToDB.DataProvider
{
	using Mapping;
	using SqlProvider;

	public class AccessDataProvider : OleDbDataProvider
	{
		private static Regex _paramsExp;

		// Based on idea from http://qapi.blogspot.com/2006/12/deriveparameters-oledbprovider-ii.html
		//
		public override bool DeriveParameters(IDbCommand command)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			if (command.CommandType != CommandType.StoredProcedure)
				throw new InvalidOperationException("command.CommandType must be CommandType.StoredProcedure");

			var conn = command.Connection as OleDbConnection;

			if (conn == null || conn.State != ConnectionState.Open)
				throw new InvalidOperationException("Invalid connection state.");

			command.Parameters.Clear();

			var dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Procedures, new object[]{null, null, command.CommandText});

			if (dt.Rows.Count == 0)
			{
				// Jet does convert parameretless procedures to views.
				//
				dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Views, new object[]{null, null, command.CommandText});

				if (dt.Rows.Count == 0)
					throw new DataException(string.Format("Stored procedure '{0}' not found", command.CommandText));

				// Do nothing. There is no parameters.
				//
			}
			else
			{
				var col = dt.Columns["PROCEDURE_DEFINITION"];

				if (col == null)
				{
					// Not really possible
					//
					return false;
				}

				if (_paramsExp == null)
					_paramsExp = new Regex(@"PARAMETERS ((\[(?<name>[^\]]+)\]|(?<name>[^\s]+))\s(?<type>[^,;\s]+(\s\([^\)]+\))?)[,;]\s)*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

				var match = _paramsExp.Match((string)dt.Rows[0][col.Ordinal]);
				var names = match.Groups["name"].Captures;
				var types = match.Groups["type"].Captures;

				if (names.Count != types.Count)
				{
					// Not really possible
					//
					return false;
				}

				var separators = new[] {' ', '(', ',', ')'};

				for (var i = 0; i < names.Count; ++i)
				{
					var paramName = names[i].Value;
					var rawType   = types[i].Value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
					var p         = new OleDbParameter(paramName, GetOleDbType(rawType[0]));

					if (rawType.Length > 2)
					{
						p.Precision = Common.ConvertTo<byte>.From(rawType[1]);
						p.Scale     = Common.ConvertTo<byte>.From(rawType[2]);
					}
					else if (rawType.Length > 1)
					{
						p.Size      = Common.ConvertTo<int>.From(rawType[1]);
					}

					command.Parameters.Add(p);
				}
			}

			return true;
		}

		private static OleDbType GetOleDbType(string jetType)
		{
			switch (jetType.ToLower())
			{
				case "byte":
				case "tinyint":
				case "integer1":
					return OleDbType.TinyInt;

				case "short":
				case "smallint":
				case "integer2":
					return OleDbType.SmallInt;

				case "int":
				case "integer":
				case "long":
				case "integer4":
				case "counter":
				case "identity":
				case "autoincrement":
					return OleDbType.Integer;

				case "single":
				case "real":
				case "float4":
				case "ieeesingle":
					return OleDbType.Single;


				case "double":
				case "number":
				case "double precision":
				case "float":
				case "float8":
				case "ieeedouble":
					return OleDbType.Double;

				case "currency":
				case "money":
					return OleDbType.Currency;

				case "dec":
				case "decimal":
				case "numeric":
					return OleDbType.Decimal;

				case "bit":
				case "yesno":
				case "logical":
				case "logical1":
					return OleDbType.Boolean;

				case "datetime":
				case "date":
				case "time":
					return OleDbType.Date;

				case "alphanumeric":
				case "char":
				case "character":
				case "character varying":
				case "national char":
				case "national char varying":
				case "national character":
				case "national character varying":
				case "nchar":
				case "string":
				case "text":
				case "varchar":
					return OleDbType.VarWChar;

				case "longchar":
				case "longtext":
				case "memo":
				case "note":
				case "ntext":
					return OleDbType.LongVarWChar;

				case "binary":
				case "varbinary":
				case "binary varying":
				case "bit varying":
					return OleDbType.VarBinary;

				case "longbinary":
				case "image":
				case "general":
				case "oleobject":
					return OleDbType.LongVarBinary;

				case "guid":
				case "uniqueidentifier":
					return OleDbType.Guid;

				default:
					// Each release of Jet brings many new aliases to existing types.
					// This list may be outdated, please send a report to us.
					//
					throw new NotSupportedException("Unknown DB type '" + jetType + "'");
			}
		}

		public override void AttachParameter(IDbCommand command, IDbDataParameter parameter)
		{
			// Do some magic to workaround 'Data type mismatch in criteria expression' error
			// in JET for some european locales.
			//
			if (parameter.Value is DateTime)
			{
				// OleDbType.DBTimeStamp is locale aware, OleDbType.Date is locale neutral.
				//
				((OleDbParameter)parameter).OleDbType = OleDbType.Date;
			}
			else if (parameter.Value is decimal)
			{
				// OleDbType.Decimal is locale aware, OleDbType.Currency is locale neutral.
				//
				((OleDbParameter)parameter).OleDbType = OleDbType.Currency;
			}

			base.AttachParameter(command, parameter);
		}

		public new const string NameString = LinqToDB.ProviderName.Access;

		public override string Name
		{
			get { return NameString; }
		}

		public override int MaxBatchSize
		{
			get { return 0; }
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new AccessSqlProvider();
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.ExceptionToErrorNumber:
					if (value is OleDbException)
					{
						var ex = (OleDbException)value;
						if (ex.Errors.Count > 0)
							return ex.Errors[0].NativeError;
					}

					break;
			}

			return SqlProvider.Convert(value, convertType);
		}

		#region DataReaderEx

		public override IDataReader GetDataReader(MappingSchemaOld schema, IDataReader dataReader)
		{
			return dataReader is OleDbDataReader?
				new DataReaderEx((OleDbDataReader)dataReader):
				base.GetDataReader(schema, dataReader);
		}

		class DataReaderEx : DataReaderBase<OleDbDataReader>, IDataReader
		{
			public DataReaderEx(OleDbDataReader rd): base(rd)
			{
			}

			public new object GetValue(int i)
			{
				var value = DataReader.GetValue(i);

				if (value is DateTime)
				{
					var dt = (DateTime)value;

					if (dt.Year == 1899 && dt.Month == 12 && dt.Day == 30)
						return new DateTime(1, 1, 1, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
				}

				return value;
			}

			public new DateTime GetDateTime(int i)
			{
				var dt = DataReader.GetDateTime(i);

				if (dt.Year == 1899 && dt.Month == 12 && dt.Day == 30)
					return new DateTime(1, 1, 1, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);

				return dt;
			}
		}

		#endregion
	}
}
