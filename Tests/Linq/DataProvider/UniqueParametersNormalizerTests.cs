using System;

using LinqToDB.DataProvider;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class UniqueParametersNormalizerTests
	{
		// Test 1: Sending a few unique strings
		[Test]
		public void TestNormalizeUniqueStrings()
		{
			var normalizer = new UniqueParametersNormalizer();
			var uniqueStrings = new[] { "test1", "test2", "test3" };

			foreach (var str in uniqueStrings)
			{
				var normalizedStr = normalizer.Normalize(str);
				Assert.AreEqual(str, normalizedStr);
			}
		}

		// Test 2: Sending some duplicated strings
		[Test]
		public void TestNormalizeDuplicatedStrings()
		{
			var normalizer = new UniqueParametersNormalizer();
			var duplicatedStrings = new[] { "test", "test", "test", "hello", "hello", "hello" };
			var expectedStrings = new[] { "test", "test_1", "test_2", "hello", "hello_1", "hello_2" };

			for (int i = 0; i < duplicatedStrings.Length; i++)
			{
				var normalizedStr = normalizer.Normalize(duplicatedStrings[i]);
				Assert.AreEqual(expectedStrings[i], normalizedStr);
			}
		}


		// Test 3: Sending a few unique strings that are 52 characters long
		[Test]
		public void TestNormalizeUniqueLongStrings()
		{
			var normalizer = new UniqueParametersNormalizer();
			var uniqueLongStrings = new[]
			{
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"bcdefghijklmnopqrstuvwxyz12345678901234567890abcdefg",
				"cdefghijklmnopqrstuvwxyz12345678901234567890abcdefgh",
			};

			var expectedStrings = new[]
			{
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcd",
				"bcdefghijklmnopqrstuvwxyz12345678901234567890abcde",
				"cdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
			};

			for (int i = 0; i < uniqueLongStrings.Length; i++)
			{
				var normalizedStr = normalizer.Normalize(uniqueLongStrings[i]);
				Assert.AreEqual(expectedStrings[i], normalizedStr);
			}
		}

		// Test 4: Sending some duplicated strings that are 52 characters long
		[Test]
		public void TestNormalizeDuplicatedLongStrings()
		{
			var normalizer = new UniqueParametersNormalizer();
			var duplicatedLongStrings = new[]
			{
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
			};
			var expectedStrings = new[]
			{
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcd",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abc",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890ab",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890ab_1",
			};

			for (int i = 0; i < duplicatedLongStrings.Length; i++)
			{
				var normalizedStr = normalizer.Normalize(duplicatedLongStrings[i]);
				Assert.AreEqual(expectedStrings[i], normalizedStr);
			}
		}

		// Test 5: Sending "abcd" string 23 times and expecting specific responses
		[Test]
		public void TestNoInfiniteLoop()
		{
			var normalizer = new TestNormalizer(3);
			var inputString = "abcd";
			var expectedStrings = new[]
			{
				"abc", "ab", "a", "a_1", "a_2", "a_3", "a_4", "a_5", "a_6", "a_7", "a_8", "a_9",
				"p", "p_1", "p_2", "p_3", "p_4", "p_5", "p_6", "p_7", "p_8", "p_9",
			};

			for (int i = 0; i < 22; i++)
			{
				var normalizedStr = normalizer.Normalize(inputString);
				Assert.AreEqual(expectedStrings[i], normalizedStr);
			}

			// Expect an InvalidOperationException when sending "abcd" an additional time
			Assert.Throws<InvalidOperationException>(() => normalizer.Normalize(inputString));
		}

		// Test 6: Sending strings with a variety of uppercase, lowercase, numbers, $ and _ characters
		[TestCase("1ABCD", "ABCD")]
		[TestCase("$abcd", "abcd")]
		[TestCase("AB1cd", "AB1cd")]
		[TestCase("AbC$", "AbC")]
		[TestCase("$$ABcD", "ABcD")]
		[TestCase("abc$$", "abc")]
		[TestCase("!@#%^&*()ABcd", "ABcd")]
		[TestCase("abC!@#%^&*()", "abC")]
		[TestCase("$!@#%^&*()aBcD", "aBcD")]
		[TestCase("Abc$!@#%^&*()", "Abc")]
		[TestCase("123", "p")]
		[TestCase("!@#%^&*()", "p")]
		[TestCase("$1$2$3", "p")]
		[TestCase("$!@#%^&*()", "p")]
		[TestCase("A1", "A1")]
		[TestCase("!", "p")]
		[TestCase("$", "p")]
		[TestCase("_ab", "ab")]
		[TestCase("_1ab", "ab")]
		[TestCase("_", "p")]
		[TestCase("AB_CD", "AB_CD")]
		[TestCase("ab_1_cd", "ab_1_cd")]
		[TestCase("$ab_cd$", "ab_cd")]
		[TestCase("_ab_cd_", "ab_cd_")]
		public void TestNormalizeSpecialCharacters(string input, string expected)
		{
			var normalizer = new UniqueParametersNormalizer();
			var normalizedStr = normalizer.Normalize(input);
			Assert.AreEqual(expected, normalizedStr);
		}

		//Test 7: Normalizing a string that does not fit on the stack
		[Test]
		public void TestNormalizeVeryLongString()
		{
			var input = new string('a', 600) + "$" + new string('b', 600);
			var normalizer = new TestNormalizer(int.MaxValue);
			var actual = normalizer.Normalize(input);
			var expected = new string('a', 600) + new string('b', 600);
			Assert.AreEqual(expected, actual);
		}

		private class TestNormalizer : UniqueParametersNormalizer
		{
			private readonly int _maxLength;
			public TestNormalizer(int maxLength)
			{
				_maxLength = maxLength;
			}
			protected override int MaxLength => _maxLength;
		}
	}
}
