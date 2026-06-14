using System.Reflection;
using System.Runtime.CompilerServices;

using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	[TestFixture]
	public class SqlTransparentExpressionTests
	{
		// Regression for the SqlTransparentExpression static field initializer that reflected for the wrong constructor
		// signature and threw TypeInitializationException on first touch of the type. Silent on desktop CLR
		// (beforefieldinit defers cctor execution; _ctor is only consumed by the "not tested" Quote() path),
		// but fatal on Mono/Android AOT which initializes the type eagerly when any of its code is touched.
		[Test]
		public void TypeInitializerDoesNotThrow()
		{
			var assembly = typeof(LinqToDBForEFTools).Assembly;
			var reader   = assembly.GetType("LinqToDB.EntityFrameworkCore.EFCoreMetadataReader");
			Assert.That(reader, Is.Not.Null, "EFCoreMetadataReader type not found — class layout changed.");

			var nested   = reader!.GetNestedType("SqlTransparentExpression", BindingFlags.NonPublic);
			Assert.That(nested, Is.Not.Null, "SqlTransparentExpression nested type not found — class layout changed.");

			Assert.DoesNotThrow(() => RuntimeHelpers.RunClassConstructor(nested!.TypeHandle));
		}
	}
}
