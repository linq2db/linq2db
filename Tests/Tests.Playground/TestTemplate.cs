using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class TestTemplate : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column]              public string? ItemId { get; set; }
			[Column(Length = 50)] public string? Value  { get; set; }
		}


		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var testItems = new SampleClass[]
			{
				new SampleClass { ItemId = "A", Value = "A-1" },
				new SampleClass { ItemId = "A", Value = "A-2" },
				new SampleClass { ItemId = "A", Value = "A-3" },
				new SampleClass { ItemId = "A", Value = "A-4" },
				new SampleClass { ItemId = "A", Value = "A-5" },
				new SampleClass { ItemId = "A", Value = "A-6" },
				new SampleClass { ItemId = "A", Value = "A-7" },
				new SampleClass { ItemId = "A", Value = "A-8" },
				new SampleClass { ItemId = "A", Value = "A-9" },
				new SampleClass { ItemId = "A", Value = "A-10" },
				new SampleClass { ItemId = "A", Value = "A-11" },
				new SampleClass { ItemId = "A", Value = "A-12" },
				new SampleClass { ItemId = "A", Value = "A-13" },
				new SampleClass { ItemId = "B", Value = "B-1" },
				new SampleClass { ItemId = "B", Value = "B-2" },
				new SampleClass { ItemId = "B", Value = "B-3" },
				new SampleClass { ItemId = "B", Value = "B-4" },
				new SampleClass { ItemId = "B", Value = "B-5" },
				new SampleClass { ItemId = "B", Value = "B-6" },
				new SampleClass { ItemId = "B", Value = "B-7" },
			};


			var groupedItems = testItems.GroupBy(ti => ti.ItemId)
				.GroupBy(o => o.Key);


			foreach (var itemsGroup in groupedItems)
			{
				Console.WriteLine($"Group: {itemsGroup.Key}");

				foreach (var chunk in itemsGroup.Chunk(5))
				{
					var chunkNumber = 0;
					foreach (var c in chunk)
					{
						Console.WriteLine($"\tChunk: {chunkNumber}");
						chunkNumber++;
						foreach (var item in c)
						{
							Console.WriteLine($"\t\tItem: {item.Value}");

						}

					}
				}
			}
		}
	}

	 public static partial class Enumerable
    {
        /// <summary>
        /// Split the elements of a sequence into chunks of size at most <paramref name="size"/>.
        /// </summary>
        /// <remarks>
        /// Every chunk except the last will be of size <paramref name="size"/>.
        /// The last chunk will contain the remaining elements and may be of a smaller size.
        /// </remarks>
        /// <param name="source">
        /// An <see cref="IEnumerable{T}"/> whose elements to chunk.
        /// </param>
        /// <param name="size">
        /// Maximum size of each chunk.
        /// </param>
        /// <typeparam name="TSource">
        /// The type of the elements of source.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> that contains the elements the input sequence split into chunks of size <paramref name="size"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="size"/> is below 1.
        /// </exception>
        public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int size)
        {

            return ChunkIterator(source, size);
        }

        private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int size)
        {
            using IEnumerator<TSource> e = source.GetEnumerator();
            while (e.MoveNext())
            {
                TSource[] chunk = new TSource[size];
                chunk[0] = e.Current;

                int i = 1;
                for (; i < chunk.Length && e.MoveNext(); i++)
                {
                    chunk[i] = e.Current;
                }

                if (i == chunk.Length)
                {
                    yield return chunk;
                }
                else
                {
                    Array.Resize(ref chunk, i);
                    yield return chunk;
                    yield break;
                }
            }
        }
    }
}
