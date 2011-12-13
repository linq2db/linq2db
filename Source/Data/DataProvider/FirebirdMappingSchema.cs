using System;

namespace LinqToDB.Data.DataProvider
{
	using Mapping;

	public class FirebirdMappingSchema : MappingSchema
	{
		public byte[] ConvertToByteArray(string value)
		{
			return System.Text.Encoding.UTF8.GetBytes(value);
		}

		public override byte[] ConvertToByteArray(object value)
		{
			if (value is string)
				return ConvertToByteArray((string)value);

			return base.ConvertToByteArray(value);
		}

		public bool ConvertToBoolean(string value)
		{
			if (value.Length == 1)
				switch (value[0])
				{
					case '1': case 'T' :case 'Y' : case 't': case 'y': return true;
					case '0': case 'F' :case 'N' : case 'f': case 'n': return false;
				}

			return Common.Convert.ToBoolean(value);
		}

		public override bool ConvertToBoolean(object value)
		{
			if (value is string)
				return ConvertToBoolean((string)value);

			return base.ConvertToBoolean(value);
		}

		public System.IO.Stream ConvertToStream(string value)
		{
			return new System.IO.MemoryStream(ConvertToByteArray(value));
		}

		public override System.IO.Stream ConvertToStream(object value)
		{
			if (value is string)
				return ConvertToStream((string)value);

			return base.ConvertToStream(value);
		}

#if !SILVERLIGHT

		public System.Data.SqlTypes.SqlBinary ConvertToSqlBinary(string value)
		{
			return Common.Convert.ToSqlBinary(ConvertToByteArray(value));
		}

		public override System.Data.SqlTypes.SqlBinary ConvertToSqlBinary(object value)
		{
			if (value is string)
				return ConvertToSqlBinary((string)value);
			return base.ConvertToSqlBinary(value);
		}

		public System.Data.SqlTypes.SqlBytes ConvertToSqlBytes(string value)
		{
			return Common.Convert.ToSqlBytes(ConvertToByteArray(value));
		}

		public override System.Data.SqlTypes.SqlBytes ConvertToSqlBytes(object value)
		{
			if (value is string)
				return ConvertToSqlBytes((string)value);

			return base.ConvertToSqlBytes(value);
		}

		public override System.Data.SqlTypes.SqlGuid ConvertToSqlGuid(object value)
		{
			if (value is string)
				return new System.Data.SqlTypes.SqlGuid(new Guid((string)value));
			return base.ConvertToSqlGuid(value);
		}

#endif

		public override bool? ConvertToNullableBoolean(object value)
		{
			if (value is string)
				return ConvertToBoolean((string)value);
			return base.ConvertToNullableBoolean(value);
		}
	}
}
