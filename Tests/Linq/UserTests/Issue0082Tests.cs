﻿using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue0082Tests : TestBase
	{
		[Test]
		public void Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var resultQuery =
					from o in db.Parent
					join od in db.Child on o.ParentID equals od.ParentID into irc
					select new
					{
						ParentID    = o.ParentID,
						CountResult = irc.Count(),
						SumResult   = irc.Sum(x => x.ParentID)
					};

				var expectedQuery =
					from o in Parent
					join od in Child on o.ParentID equals od.ParentID into irc
					select new
					{
						ParentID    = o.ParentID,
						CountResult = irc.Count(),
						SumResult   = irc.Sum(x => x.ParentID)
					};

				AreEqual(expectedQuery, resultQuery);

				Assert.That(expectedQuery.Count(), Is.EqualTo(resultQuery.Count()));

				AreEqual(
					expectedQuery.Where(x => x.CountResult > 0),
					resultQuery  .Where(x => x.CountResult > 0));
			}
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{

				var resultQuery =
							from o in db.Parent
							select new
							{
								ParentID    = o.ParentID,
								CountResult = o.Children.Count(),
								SumResult   = o.Children.Sum(x => x.ParentID)
							};

				var expectedQuery =
							from o in Parent
							select new
							{
								ParentID    = o.ParentID,
								CountResult = o.Children.Count(),
								SumResult   = o.Children.Sum(x => x.ParentID)
							};

				AreEqual(expectedQuery, resultQuery);

				Assert.That(expectedQuery.Count(), Is.EqualTo(resultQuery.Count()));

				AreEqual(expectedQuery.Where(x => x.CountResult > 0),
					     resultQuery  .Where(x => x.CountResult > 0));
			}
		}

	}
}
