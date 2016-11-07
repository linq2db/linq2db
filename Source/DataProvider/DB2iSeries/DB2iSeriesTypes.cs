using System;

namespace LinqToDB.DataProvider.DB2iSeries {

  public static class DB2iSeriesTypes {
    // https://secure.pamtransport.com/bin/IBM.Data.DB2.iSeries.xml

    public static readonly TypeCreator<long> BigInt = new TypeCreator<long>();
    public static readonly TypeCreator<byte[]> Binary = new TypeCreator<byte[]>();
    public static readonly DB2iSeriesTypeCreator<byte[]> Blob = new DB2iSeriesTypeCreator<byte[]>();
    public static readonly TypeCreator<string> Char = new TypeCreator<string>();
    public static readonly TypeCreator<byte[]> CharBitData = new TypeCreator<byte[]>();
    public static readonly DB2iSeriesTypeCreator<string> Clob = new DB2iSeriesTypeCreator<string>();
    public static readonly TypeCreator<string> DataLink = new TypeCreator<string>();
    public static readonly TypeCreator<DateTime> Date = new TypeCreator<DateTime>();
    public static readonly DB2iSeriesTypeCreator<string> DbClob = new DB2iSeriesTypeCreator<string>();
    public static readonly TypeCreator<decimal, double, long> DecFloat16 = new TypeCreator<decimal, double, long>();
    public static readonly TypeCreator<decimal, double, long> DecFloat34 = new TypeCreator<decimal, double, long>();
    public static readonly TypeCreator<decimal> Decimal = new TypeCreator<decimal>();
    public static readonly TypeCreator<double> Double = new TypeCreator<double>();
    public static readonly TypeCreator<string> Graphic = new TypeCreator<string>();
    public static readonly TypeCreator<int> Integer = new TypeCreator<int>();
    public static readonly TypeCreator<decimal> Numeric = new TypeCreator<decimal>();
    public static readonly TypeCreator<float> Real = new TypeCreator<float>();
    public static readonly TypeCreator<byte[]> RowId = new TypeCreator<byte[]>();
    public static readonly TypeCreator<short> SmallInt = new TypeCreator<short>();
    public static readonly TypeCreator<DateTime> Time = new TypeCreator<DateTime>();
    public static readonly TypeCreator<DateTime> TimeStamp = new TypeCreator<DateTime>();
    public static readonly TypeCreator<byte[]> VarBinary = new TypeCreator<byte[]>();
    public static readonly TypeCreator<string> VarChar = new TypeCreator<string>();
    public static readonly TypeCreator<byte[]> VarCharBitData = new TypeCreator<byte[]>();
    public static readonly TypeCreator<string> VarGraphic = new TypeCreator<string>();
    public static readonly TypeCreator<string> Xml = new TypeCreator<string>();
    public static Type ConnectionType { get; set; }

  }
}