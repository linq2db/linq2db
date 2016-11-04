using System;

namespace LinqToDB.DataProvider.DB2 {

  public class DB2Types {
    public static readonly TypeCreator<long> DB2Int64 = new TypeCreator<long>();
    public static readonly TypeCreator<int> DB2Int32 = new TypeCreator<int>();
    public static readonly TypeCreator<short> DB2Int16 = new TypeCreator<short>();
    public static readonly TypeCreator<decimal> DB2Decimal = new TypeCreator<decimal>();
    public static readonly TypeCreator<decimal, double, long> DB2DecimalFloat = new TypeCreator<decimal, double, long>();
    public static readonly TypeCreator<float> DB2Real = new TypeCreator<float>();
    public static readonly TypeCreator<double> DB2Real370 = new TypeCreator<double>();
    public static readonly TypeCreator<double> DB2Double = new TypeCreator<double>();
    public static readonly TypeCreator<string> DB2String = new TypeCreator<string>();
    public static readonly DB2TypeCreator<string> DB2Clob = new DB2TypeCreator<string>();
    public static readonly TypeCreator<byte[]> DB2Binary = new TypeCreator<byte[]>();
    public static readonly DB2TypeCreator<byte[]> DB2Blob = new DB2TypeCreator<byte[]>();
    public static readonly TypeCreator<DateTime> DB2Date = new TypeCreator<DateTime>();
    public static readonly TypeCreator<DateTime, long> DB2DateTime = new TypeCreator<DateTime, long>();
    public static readonly TypeCreator<TimeSpan> DB2Time = new TypeCreator<TimeSpan>();
    public static readonly TypeCreator<DateTime> DB2TimeStamp = new TypeCreator<DateTime>();
    public static Type DB2Xml;
    public static readonly TypeCreator DB2RowId = new TypeCreator();

    public static Type ConnectionType { get; internal set; }
  }
}
