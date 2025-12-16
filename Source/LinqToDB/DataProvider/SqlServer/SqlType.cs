using System;
using System.Globalization;

namespace LinqToDB.DataProvider.SqlServer
{
	/// <summary>
	/// See <see href="https://docs.microsoft.com/en-us/sql/t-sql/data-types/data-types-transact-sql">Data types</see>.
	/// </summary>
	public abstract class SqlType
	{
		protected SqlType(string dataType)
		{
			_dataType = dataType;
		}

		readonly string _dataType;

		public override string ToString()
		{
			return _dataType;
		}

		sealed class SqlExpression : Sql.ExpressionAttribute
		{
			public SqlExpression(string name) : base(ProviderName.SqlServer, name)
			{
			}
		}

		// Exact numerics
		//
		[SqlExpression("bigint")]              public static SqlType<long?>           BigInt                            => new SqlType<long?>   ("bigint");
		[SqlExpression("numeric")]             public static SqlType<decimal?>        Numeric()                         => new SqlType<decimal?>("numeric");
		[SqlExpression("numeric({0})")]        public static SqlType<decimal?>        Numeric(int precision)            => new SqlType<decimal?>(string.Create(CultureInfo.InvariantCulture, $"numeric({precision})"));
		[SqlExpression("numeric({0}, {1})")]   public static SqlType<decimal?>        Numeric(int precision, int scale) => new SqlType<decimal?>(string.Create(CultureInfo.InvariantCulture, $"numeric({precision}, {scale})"));
		[SqlExpression("bit")]                 public static SqlType<bool?>           Bit                               => new SqlType<bool?>   ("bit");
		[SqlExpression("smallint")]            public static SqlType<short?>          SmallInt                          => new SqlType<short?>  ("smallint");
		[SqlExpression("decimal")]             public static SqlType<decimal?>        Decimal()                         => new SqlType<decimal?>("decimal");
		[SqlExpression("decimal({0})")]        public static SqlType<decimal?>        Decimal(int precision)            => new SqlType<decimal?>(FormattableString.Invariant($"decimal({precision})"));
		[SqlExpression("decimal({0}, {1})")]   public static SqlType<decimal?>        Decimal(int precision, int scale) => new SqlType<decimal?>(FormattableString.Invariant($"decimal({precision}, {scale})"));
		[SqlExpression("smallmoney")]          public static SqlType<decimal?>        SmallMoney                        => new SqlType<decimal?>("smallmoney");
		[SqlExpression("int")]                 public static SqlType<int?>            Int                               => new SqlType<int?>    ("int");
		[SqlExpression("tinyint")]             public static SqlType<byte?>           TinyInt                           => new SqlType<byte?>   ("tinyint");
		[SqlExpression("money")]               public static SqlType<decimal?>        Money                             => new SqlType<decimal?>("money");

		// Approximate numerics
		//
		[SqlExpression("float")]               public static SqlType<double?>         Float()                           => new SqlType<double?>  ("float");
		[SqlExpression("float({0})")]          public static SqlType<double?>         Float(int n)                      => new SqlType<double?>  (FormattableString.Invariant($"float({n})"));
		[SqlExpression("real")]                public static SqlType<float?>          Real                              => new SqlType<float?>   ("real");

		// Date and Time
		//
		[SqlExpression("date")]                public static SqlType<DateTime?>       Date                              => new SqlType<DateTime?>      ("date");
		[SqlExpression("datetimeoffset")]      public static SqlType<DateTimeOffset?> DatetimeOffset()                  => new SqlType<DateTimeOffset?>("datetimeoffset");
		[SqlExpression("datetimeoffset({0})")] public static SqlType<DateTimeOffset?> DatetimeOffset(int size)          => new SqlType<DateTimeOffset?>(FormattableString.Invariant($"datetimeoffset({size})"));
		[SqlExpression("datetime2")]           public static SqlType<DateTime?>       Datetime2()                       => new SqlType<DateTime?>      ("datetime2");
		[SqlExpression("datetime2({0})")]      public static SqlType<DateTime?>       Datetime2(int size)               => new SqlType<DateTime?>      (FormattableString.Invariant($"datetime2({size})"));
		[SqlExpression("smalldatetime")]       public static SqlType<DateTime?>       SmallDatetime                     => new SqlType<DateTime?>      ("smalldatetime");
		[SqlExpression("datetime")]            public static SqlType<DateTime?>       Datetime                          => new SqlType<DateTime?>      ("datetime");
		[SqlExpression("time")]                public static SqlType<TimeSpan?>       Time()                            => new SqlType<TimeSpan?>      ("time");
		[SqlExpression("time({0})")]           public static SqlType<TimeSpan?>       Time(int size)                    => new SqlType<TimeSpan?>      (FormattableString.Invariant($"time({size})"));

		// Character strings
		//
		[SqlExpression("char")]                public static SqlType<string?>         Char()                            => new SqlType<string?>("char");
		[SqlExpression("char({0})")]           public static SqlType<string?>         Char(int size)                    => new SqlType<string?>(FormattableString.Invariant($"char({size})"));
		[SqlExpression("char(max)")]           public static SqlType<string?>         CharMax                           => new SqlType<string?>("char(max)");
		[SqlExpression("varchar")]             public static SqlType<string?>         VarChar()                         => new SqlType<string?>("varchar");
		[SqlExpression("varchar({0})")]        public static SqlType<string?>         VarChar(int size)                 => new SqlType<string?>(FormattableString.Invariant($"varchar({size})"));
		[SqlExpression("varchar(max)")]        public static SqlType<string?>         VarCharMax                        => new SqlType<string?>("varchar(max)");
		[SqlExpression("text")]                public static SqlType<string?>         Text                              => new SqlType<string?>("text");

		// Unicode character strings
		//
		[SqlExpression("nchar")]               public static SqlType<string?>         NChar()                           => new SqlType<string?>("nchar");
		[SqlExpression("nchar({0})")]          public static SqlType<string?>         NChar(int size)                   => new SqlType<string?>(FormattableString.Invariant($"nchar({size})"));
		[SqlExpression("nchar(max)")]          public static SqlType<string?>         NCharMax                          => new SqlType<string?>("nchar(max)");
		[SqlExpression("nvarchar")]            public static SqlType<string?>         NVarChar()                        => new SqlType<string?>("nvarchar");
		[SqlExpression("nvarchar({0})")]       public static SqlType<string?>         NVarChar(int size)                => new SqlType<string?>(FormattableString.Invariant($"nvarchar({size})"));
		[SqlExpression("nvarchar(max)")]       public static SqlType<string?>         NVarCharMax                       => new SqlType<string?>("nvarchar(max)");
		[SqlExpression("ntext")]               public static SqlType<string?>         NText                             => new SqlType<string?>("ntext");

		// Binary strings
		[SqlExpression("binary")]              public static SqlType<byte[]?>         Binary()                          => new SqlType<byte[]?>("binary");
		[SqlExpression("binary({0})")]         public static SqlType<byte[]?>         Binary(int size)                  => new SqlType<byte[]?>(FormattableString.Invariant($"binary({size})"));
		[SqlExpression("binary(max)")]         public static SqlType<byte[]?>         BinaryMax                         => new SqlType<byte[]?>("binary(max)");
		[SqlExpression("varbinary")]           public static SqlType<byte[]?>         VarBinary()                       => new SqlType<byte[]?>("varbinary");
		[SqlExpression("varbinary({0})")]      public static SqlType<byte[]?>         VarBinary(int size)               => new SqlType<byte[]?>(FormattableString.Invariant($"varbinary({size})"));
		[SqlExpression("varbinary(max)")]      public static SqlType<byte[]?>         VarBinaryMax                      => new SqlType<byte[]?>("varbinary(max)");
		[SqlExpression("image")]               public static SqlType<byte[]?>         Image                             => new SqlType<byte[]?>("image");

		// Other data types
		//
		[SqlExpression("cursor")]              public static SqlType<object?>         Cursor                            => new SqlType<object?>("cursor");
		[SqlExpression("rowversion")]          public static SqlType<byte[]?>         RowVersion                        => new SqlType<byte[]?>("rowversion");
		[SqlExpression("hierarchyid")]         public static SqlType<object?>         HierarchyID()                     => new SqlType<object?>("hierarchyid");
		[SqlExpression("hierarchyid")]         public static SqlType<T>               HierarchyID<T>()                  => new SqlType<T>      ("hierarchyid");
		[SqlExpression("uniqueidentifier")]    public static SqlType<Guid?>           UniqueIdentifier                  => new SqlType<Guid?>  ("uniqueidentifier");
		[SqlExpression("sql_variant")]         public static SqlType<object?>         SqlVariant                        => new SqlType<object?>("sql_variant");
		[SqlExpression("xml")]                 public static SqlType<string?>         Xml()                             => new SqlType<string?>("xml");
		[SqlExpression("xml")]                 public static SqlType<T>               Xml<T>()                          => new SqlType<T>      ("xml");
		[SqlExpression("geometry")]            public static SqlType<object?>         Geometry()                        => new SqlType<object?>("geometry");
		[SqlExpression("geometry")]            public static SqlType<T>               Geometry<T>()                     => new SqlType<T>      ("geometry");
		[SqlExpression("geography")]           public static SqlType<object?>         Geography()                       => new SqlType<object?>("geography");
		[SqlExpression("geography")]           public static SqlType<T>               Geography<T>()                    => new SqlType<T>      ("geography");
		[SqlExpression("table")]               public static SqlType<object?>         Table                             => new SqlType<object?>("table");

		// Vectors
		[SqlExpression("vector({0},float32)")] public static SqlType<float[]?>        Vector32(int size)                => new (FormattableString.Invariant($"vector({size},float32)"));
#if NET8_0_OR_GREATER
		[SqlExpression("vector({0},float16)")] public static SqlType<Half[]?>         Vector16(int size)                => new (FormattableString.Invariant($"vector({size},float16)"));
#endif
	}

	/// <summary>
	/// See <see href="https://docs.microsoft.com/en-us/sql/t-sql/data-types/data-types-transact-sql">Data types</see>.
	/// </summary>
	public class SqlType<T> : SqlType
	{
		public SqlType(string dataType)
			: base(dataType)
		{
		}
	}
}
