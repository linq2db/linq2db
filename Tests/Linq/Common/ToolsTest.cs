using System.IO;
using System.Runtime.InteropServices;
using LinqToDB.Common;

using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class ToolsTest
	{
		[Test]
		public void AssemblyPathTest()
		{
			var asm = typeof(ToolsTest).Assembly;

			var path = asm.GetPath();
			var file = asm.GetFileName();

			Assert.IsNotEmpty(path);
			Assert.IsNotEmpty(file);

			Assert.That(File     .Exists(file));
			Assert.That(Directory.Exists(path));
		}
	}
}
