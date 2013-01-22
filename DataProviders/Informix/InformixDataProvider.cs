using System;
using System.Data;
using System.Globalization;
using System.Threading;
using IBM.Data.Informix;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	public class InformixDataProvider : DataProviderBase
	{
		public InformixDataProvider() : base(new InformixMappingSchema())
		{
			SqlProviderFlags.IsParameterOrderDependent = true;

			SetCharField("CHAR",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("NCHAR", (r,i) => r.GetString(i).TrimEnd());

			SetField<IfxDataReader,Int64>("BIGINT", (r,i) => r.GetBigInt(i));

			SetProviderField<IfxDataReader,IfxDecimal, decimal> ((r,i) => r.GetIfxDecimal (i));
			SetProviderField<IfxDataReader,IfxDateTime,DateTime>((r,i) => r.GetIfxDateTime(i));
			SetProviderField<IfxDataReader,IfxTimeSpan,TimeSpan>((r,i) => r.GetIfxTimeSpan(i));

			SetProviderField<IDataReader,float,  float  >((r,i) => GetFloat  (r, i));
			SetProviderField<IDataReader,double, double >((r,i) => GetDouble (r, i));
			SetProviderField<IDataReader,decimal,decimal>((r,i) => GetDecimal(r, i));
		}

		static float GetFloat(IDataReader dr, int idx)
		{
			var current = Thread.CurrentThread.CurrentCulture;

			if (Thread.CurrentThread.CurrentCulture != CultureInfo.InvariantCulture)
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			var value = dr.GetFloat(idx);

			if (current != CultureInfo.InvariantCulture)
				Thread.CurrentThread.CurrentCulture = current;

			return value;
		}

		static double GetDouble(IDataReader dr, int idx)
		{
			var current = Thread.CurrentThread.CurrentCulture;

			if (Thread.CurrentThread.CurrentCulture != CultureInfo.InvariantCulture)
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			var value = dr.GetDouble(idx);

			if (current != CultureInfo.InvariantCulture)
				Thread.CurrentThread.CurrentCulture = current;

			return value;
		}

		static decimal GetDecimal(IDataReader dr, int idx)
		{
			var current = Thread.CurrentThread.CurrentCulture;

			if (Thread.CurrentThread.CurrentCulture != CultureInfo.InvariantCulture)
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			var value = dr.GetDecimal(idx);

			if (current != CultureInfo.InvariantCulture)
				Thread.CurrentThread.CurrentCulture = current;

			return value;
		}

		public override string Name           { get { return ProviderName.DB2;      } }
		public override Type   ConnectionType { get { return typeof(IfxConnection); } }
		public override Type   DataReaderType { get { return typeof(IfxDataReader); } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new IfxConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new InformixSqlProvider();
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (value is TimeSpan)
				value = new IfxTimeSpan((TimeSpan)value);
			else if (value is Guid)
			{
				value    = value.ToString();
				dataType = DataType.Char;
			}
			else if (value is bool)
			{
				value = (bool)value ? 't' : 'f';
				dataType = DataType.Char;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.UInt16    : dataType = DataType.Int32;    break;
				case DataType.UInt32    : dataType = DataType.Int64;    break;
				case DataType.UInt64    : dataType = DataType.Decimal;  break;
				case DataType.VarNumeric: dataType = DataType.Decimal;  break;
				case DataType.DateTime2 : dataType = DataType.DateTime; break;
				case DataType.Text      : ((IfxParameter)parameter).IfxType = IfxType.Clob; return;
				case DataType.NText     : ((IfxParameter)parameter).IfxType = IfxType.Clob; return;
			}

			base.SetParameterType(parameter, dataType);
		}
	}
}
