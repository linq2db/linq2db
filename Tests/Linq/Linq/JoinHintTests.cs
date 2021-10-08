using System;
using System.Linq;
using LinqToDB;
using NUnit.Framework;

namespace Tests.Linq
{
	public class JoinHintTests : TestBase
	{
		[Test]
		public void JoinWithHintSyntax1(
			[IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.Join(
						db.Doctor,
						SqlJoinType.Inner,
						SqlJoinHint.Loop,
						(p, d) => p.ID == d.PersonID,
						(p, d) => p.ID)
					.ToString()!;
                
				CompareSql(@"
SELECT
	[p].[PersonID]
FROM
	[Person] [p]
		INNER LOOP JOIN [Doctor] [d] ON [p].[PersonID] = [d].[PersonID]
", query);
			}
		}
		
		[Test]
		public void JoinWithHintSyntax2(
			[IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = 
					(from d in db.Doctor
					from p in db.Person
						.Join(SqlJoinType.Inner, SqlJoinHint.Loop, p => p.ID == d.PersonID)
					select new { d.PersonID, p.LastName })
					.ToString()!;
                
				CompareSql(@"
SELECT
	[d].[PersonID],
	[p].[LastName]
FROM
	[Doctor] [d]
		INNER LOOP JOIN [Person] [p] ON [p].[PersonID] = [d].[PersonID]
", query);
			}
		}

		[Test]
		public void JoinHintWithDifferentJoinTypes(
			[IncludeDataSources(TestProvName.AllSqlServer)] string context,
			[Values(SqlJoinType.Inner, SqlJoinType.Left, SqlJoinType.Right, SqlJoinType.Full)] SqlJoinType joinType)
		{
			var type = joinType switch
			{
				SqlJoinType.Inner => "INNER",
				SqlJoinType.Left => "LEFT",
				SqlJoinType.Right => "RIGHT",
				SqlJoinType.Full => "FULL",
				_ => throw new ArgumentOutOfRangeException(nameof(joinType), joinType, null)
			};
			
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.Join(
						db.Doctor,
						joinType,
						SqlJoinHint.Loop,
						(p, d) => p.ID == d.PersonID,
						(p, d) => p.ID)
					.ToString()!;
                
				CompareSql(@$"
SELECT
	[p].[PersonID]
FROM
	[Person] [p]
		{type} LOOP JOIN [Doctor] [d] ON [p].[PersonID] = [d].[PersonID]
", query);
			}
		}

		[Test]
		public void JoinHintWithDifferentJoinHints(
			[IncludeDataSources(TestProvName.AllSqlServer)] string context,
			[Values(SqlJoinHint.Hash, SqlJoinHint.Loop, SqlJoinHint.Merge, SqlJoinHint.Remote)] SqlJoinHint joinHint
		)
		{
			var hint = joinHint switch
			{
				SqlJoinHint.Hash => "HASH",
				SqlJoinHint.Loop => "LOOP",
				SqlJoinHint.Merge => "MERGE",
				SqlJoinHint.Remote => "REMOTE",
				_ => throw new ArgumentOutOfRangeException(nameof(joinHint), joinHint, null)
			};
			
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.Join(
						db.Doctor,
						SqlJoinType.Inner,
						joinHint,
						(p, d) => p.ID == d.PersonID,
						(p, d) => p.ID)
					.ToString()!;
                
				CompareSql(@$"
SELECT
	[p].[PersonID]
FROM
	[Person] [p]
		INNER {hint} JOIN [Doctor] [d] ON [p].[PersonID] = [d].[PersonID]
", query);
			}
		}

		[Test]
		public void MultipleJoins1(
			[IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.Join(
						db.Doctor,
						SqlJoinType.Inner,
						(p, d) => p.ID == d.PersonID,
						(p, d) => d)
					.Join(
						db.Child,
						SqlJoinType.Inner,
						SqlJoinHint.Loop,
						(d, c) => d.PersonID == c.ChildID,
						(d, c) => d)
					.ToString()!;
				
				CompareSql(@$"
SELECT
	[d].[PersonID],
	[d].[Taxonomy]
FROM
	[Person] [p]
		INNER JOIN [Doctor] [d] ON [p].[PersonID] = [d].[PersonID]
		INNER LOOP JOIN [Child] [c_1] ON [d].[PersonID] = [c_1].[ChildID]
", query);
			}
		}
		
		[Test]
		public void MultipleJoins2(
			[IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.Join(
						db.Doctor,
						SqlJoinType.Inner,
						SqlJoinHint.Loop,
						(p, d) => p.ID == d.PersonID,
						(p, d) => d)
					.Join(
						db.Child,
						SqlJoinType.Inner,
						(d, c) => d.PersonID == c.ChildID,
						(d, c) => d)
					.ToString()!;
				
				CompareSql(@$"
SELECT
	[d].[PersonID],
	[d].[Taxonomy]
FROM
	[Person] [p]
		INNER LOOP JOIN [Doctor] [d] ON [p].[PersonID] = [d].[PersonID]
		INNER JOIN [Child] [c_1] ON [d].[PersonID] = [c_1].[ChildID]
", query);
			}
		}
		
		[Test]
		public void MultipleJoins3(
			[IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = 
					(from d in db.Doctor
						from p in db.Person
							.Join(SqlJoinType.Inner, SqlJoinHint.Loop, p => p.ID == d.PersonID)
						from c in db.Child
							.Join(SqlJoinType.Inner, c => c.ParentID == p.ID)
						select new { d.PersonID, p.LastName })
					.ToString()!;
				
				CompareSql(@$"
SELECT
	[d].[PersonID],
	[p].[LastName]
FROM
	[Doctor] [d]
		INNER LOOP JOIN [Person] [p] ON [p].[PersonID] = [d].[PersonID]
		INNER JOIN [Child] [c_1] ON [c_1].[ParentID] = [p].[PersonID]
", query);
			}
		}
		
		[Test]
		public void MultipleJoins4(
			[IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = 
					(from d in db.Doctor
						from p in db.Person
							.InnerJoin(p => p.ID == d.PersonID)
						from c in db.Child
							.Join(SqlJoinType.Inner, SqlJoinHint.Loop, c => c.ParentID == p.ID)
						select new { d.PersonID, p.LastName })
					.ToString()!;
				
				CompareSql(@$"
SELECT
	[d].[PersonID],
	[p].[LastName]
FROM
	[Doctor] [d]
		INNER JOIN [Person] [p] ON [p].[PersonID] = [d].[PersonID]
		INNER LOOP JOIN [Child] [c_1] ON [c_1].[ParentID] = [p].[PersonID]
", query);
			}
		}

		[Test]
		public void AnotherProvidersTest(
			[DataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				string query = db.Person
					.Join(
						db.Doctor,
						SqlJoinType.Inner,
						SqlJoinHint.Loop,
						(p, d) => p.ID == d.PersonID,
						(p, d) => p.ID)
					.ToString()!;

				Assert.False(query.ToLowerInvariant().Contains("loop"));
			}
		}
	}
}
