namespace LinqToDB.EntityFrameworkCore.Tests.Models.JsonConverter
{
#pragma warning disable CA1028 // Enum Storage should be Int32
	public enum CrashEnum : byte
#pragma warning restore CA1028 // Enum Storage should be Int32
	{
		OneValue = 0,
		OtherValue = 1
	}
}
