using System.Text;

using LinqToDB.Internal.DataProvider;

using NUnit.Framework;

using Shouldly;

namespace Tests.Infrastructure
{
	[TestFixture]
	public class IdentifierServiceTests : TestBase
	{
		// 40 Japanese characters: 40 chars fits a 63 *character* limit, 120 bytes busts a 63 *byte* one
		const string Japanese40 = "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらり";

		[Test]
		public void CharacterLimit_CountsCharacters()
		{
			var service = new IdentifierServiceSimple(63);

			service.IsFit(IdentifierKind.Alias, Japanese40, out var sizeDecrement).ShouldBeTrue();
			sizeDecrement.ShouldBeNull();
		}

		[Test]
		public void ByteLimit_CountsUtf8Bytes()
		{
			var service = new IdentifierServiceSimple(63, IdentifierLengthUnit.Utf8Bytes);

			// 40 characters is under the limit, 120 bytes is not
			service.IsFit(IdentifierKind.Alias, Japanese40, out var sizeDecrement).ShouldBeFalse();
			sizeDecrement.ShouldNotBeNull();

			// sizeDecrement is a count of characters to drop, and what is left has to fit
			var kept = Japanese40.Length - sizeDecrement!.Value;
			Encoding.UTF8.GetByteCount(Japanese40.Substring(0, kept)).ShouldBeLessThanOrEqualTo(63);
		}

		[Test]
		public void ByteLimit_AsciiBehavesLikeCharacterLimit()
		{
			var ascii   = new string('a', 40);
			var service = new IdentifierServiceSimple(63, IdentifierLengthUnit.Utf8Bytes);

			service.IsFit(IdentifierKind.Alias, ascii, out _).ShouldBeTrue();
		}

		[Test]
		public void TruncateIdentifier_ByteLimit_ResultFitsWithHeadroom()
		{
			var service    = new IdentifierServiceSimple(63, IdentifierLengthUnit.Utf8Bytes);
			var truncated  = IdentifiersHelper.TruncateIdentifier(service, IdentifierKind.Alias, Japanese40);

			truncated.Length.ShouldBeLessThan(Japanese40.Length);
			// four units of headroom are reserved for the uniquifying suffix
			Encoding.UTF8.GetByteCount(truncated).ShouldBeLessThanOrEqualTo(63 - 4);
			Japanese40.ShouldStartWith(truncated);
		}

		[Test]
		public void TruncateIdentifier_CharacterLimit_TruncatesToLimitLessHeadroom()
		{
			var service   = new IdentifierServiceSimple(63);
			var identifier = new string('a', 100);

			IdentifiersHelper.TruncateIdentifier(service, IdentifierKind.Alias, identifier).Length.ShouldBe(59);
		}

		[Test]
		public void TruncateIdentifier_NeverSplitsASurrogatePair()
		{
			// 40 astral characters, 4 UTF-8 bytes and 2 UTF-16 chars each
			var identifier = string.Concat(System.Linq.Enumerable.Repeat("\U0001F600", 40));

			foreach (var unit in new[] { IdentifierLengthUnit.Characters, IdentifierLengthUnit.Utf8Bytes })
			{
				var service   = new IdentifierServiceSimple(63, unit);
				var truncated = IdentifiersHelper.TruncateIdentifier(service, IdentifierKind.Alias, identifier);

				truncated.Length.ShouldBeGreaterThan(0);
				char.IsHighSurrogate(truncated[truncated.Length - 1]).ShouldBeFalse();
			}
		}
	}
}
