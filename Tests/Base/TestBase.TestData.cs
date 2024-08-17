using System;

namespace Tests
{
	public partial class TestBase
	{
		protected static class TestData
		{
			// offset 40 is not used by any timezone, so we can detect tz handling issues, which could be hidden when offset match current TZ
			public static readonly DateTimeOffset DateTimeOffset          = new DateTimeOffset(2020, 2, 29, 17, 54, 55, 123, TimeSpan.FromMinutes(40)).AddTicks(1234);
			public static readonly DateTimeOffset DateTimeOffsetUtc       = new DateTimeOffset(2020, 2, 29, 17, 9, 55, 123, TimeSpan.Zero).AddTicks(1234);
			public static readonly DateTime DateTime                      = new DateTime(2020, 2, 29, 17, 54, 55, 123).AddTicks(1234);
			public static readonly DateTime DateTime0                     = new DateTime(2020, 2, 29, 17, 54, 55);
			public static readonly DateTime DateTime3                     = new DateTime(2020, 2, 29, 17, 54, 55, 123);
			public static readonly DateTime DateTimeUtc                   = new DateTime(2020, 2, 29, 17, 54, 55, 123, DateTimeKind.Utc).AddTicks(1234);
			public static readonly DateTime DateTime4Utc                  = new DateTime(2020, 2, 29, 17, 54, 55, 123, DateTimeKind.Utc).AddTicks(1000);
			public static readonly DateTime Date                          = new (2020, 2, 29);
			public static readonly DateTime DateAmbiguous                 = new (2020, 8, 9);
#if NET6_0_OR_GREATER
			public static readonly DateOnly DateOnly                      = new (2020, 2, 29);
			public static readonly DateOnly DateOnlyAmbiguous             = new (2020, 8, 9);
#endif
			public static readonly TimeSpan TimeOfDay                     = new TimeSpan(0, 17, 54, 55, 123).Add(TimeSpan.FromTicks(1234));
			public static readonly TimeSpan TimeOfDay4                    = new TimeSpan(0, 17, 54, 55, 123).Add(TimeSpan.FromTicks(1000));
			public static readonly Guid     Guid1                         = new ("bc7b663d-0fde-4327-8f92-5d8cc3a11d11");
			public static readonly Guid     Guid2                         = new ("a948600d-de21-4f74-8ac2-9516b287076e");
			public static readonly Guid     Guid3                         = new ("bd3973a5-4323-4dd8-9f4f-df9f93e2a627");
			public static readonly Guid     Guid4                         = new ("76b1c875-2287-4b82-a23b-7967c5eafed8");
			public static readonly Guid     Guid5                         = new ("656606a4-6e36-4431-add6-85f886a1c7c2");
			public static readonly Guid     Guid6                         = new ("66aa9df9-260f-4a2b-ac50-9ca8ce7ad725");

			public static byte[] Binary(int size)
			{
				var value = new byte[size];
				for (var i = 0; i < value.Length; i++)
					value[i] = (byte)(i % 256);

				return value;
			}

			public static Guid SequentialGuid(int n) => new($"233bf399-9710-4e79-873d-2ec7bf1e{n:x4}");
		}
	}
}
