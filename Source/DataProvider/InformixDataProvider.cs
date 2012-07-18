using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading;

using IBM.Data.Informix;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	class InformixDataProvider :  DataProviderBaseOld
	{
		public override IDbConnection CreateConnectionObject () { return new IfxConnection      (); }
		public override DbDataAdapter CreateDataAdapterObject() { return new IfxDataAdapter     (); }
		public override ISqlProvider  CreateSqlProvider      () { return new InformixSqlProvider(); }

		public override Type   ConnectionType { get { return typeof(IfxConnection);          } }
		public override string Name           { get { return LinqToDB.ProviderName.Informix; } }
		public override string EndOfSql       { get { return ";"; } }

		public override bool DeriveParameters(IDbCommand command)
		{
			if (command is IfxCommand)
			{
				IfxCommandBuilder.DeriveParameters((IfxCommand)command);
				return true;
			}

			return false;
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.ExceptionToErrorNumber:
					if (value is IfxException)
					{
						var ex = (IfxException)value;

						foreach (IfxError error in ex.Errors)
							return error.NativeError;

						return 0;
					}

					break;
			}

			return SqlProvider.Convert(value, convertType);
		}

		public override void PrepareCommand(ref CommandType commandType, ref string commandText, ref IDbDataParameter[] commandParameters)
		{
			base.PrepareCommand(ref commandType, ref commandText, ref commandParameters);

			if (commandParameters != null) foreach (var p in commandParameters)
			{
				if (p.Value is Guid)
				{
					var value = p.Value.ToString();
					p.DbType = DbType.AnsiStringFixedLength;
					p.Value  = value;
					p.Size   = value.Length;
				}
				else if (p.Value is bool)
				{
					p.Value = ((InformixSqlProvider)SqlProvider).ConvertBooleanValue((bool)p.Value);
				}
				//else if (p.DbType == DbType.Binary)
				//{
				//	var ip = (IfxParameter)p;

				//	ip.IfxType = IfxType.Blob;
				//}
			}
		}

		#region GetDataReader

		public override IDataReader GetDataReader(Mapping.MappingSchemaOld schema, IDataReader dataReader)
		{
			return dataReader is IfxDataReader?
				new InformixDataReaderEx((IfxDataReader)dataReader):
				base.GetDataReader(schema, dataReader);
		}

		class InformixDataReaderEx : DataReaderBase<IfxDataReader>, IDataReader
		{
			public InformixDataReaderEx(IfxDataReader rd): base(rd)
			{
			}

			public new float GetFloat(int i)
			{
				var current = Thread.CurrentThread.CurrentCulture;

				if (Thread.CurrentThread.CurrentCulture != CultureInfo.InvariantCulture)
					Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				var value = DataReader.GetFloat(i);

				if (current != CultureInfo.InvariantCulture)
					Thread.CurrentThread.CurrentCulture = current;

				return value;
			}

			public new double GetDouble(int i)
			{
				var current = Thread.CurrentThread.CurrentCulture;

				if (Thread.CurrentThread.CurrentCulture != CultureInfo.InvariantCulture)
					Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				var value = DataReader.GetDouble(i);

				if (current != CultureInfo.InvariantCulture)
					Thread.CurrentThread.CurrentCulture = current;

				return value;
			}

			public new decimal GetDecimal(int i)
			{
				var current = Thread.CurrentThread.CurrentCulture;

				if (Thread.CurrentThread.CurrentCulture != CultureInfo.InvariantCulture)
					Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				var value = DataReader.GetDecimal     (i);

				if (current != CultureInfo.InvariantCulture)
					Thread.CurrentThread.CurrentCulture = current;

				return value;
			}
		}

		#endregion
	}
}
