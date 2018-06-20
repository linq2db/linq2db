using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1196Tests : TestBase
	{
	    [Table(Name ="Requests")]
	    class Request
	    {
	        [Column] public int Id { get; set; }
	        [Column] public int FirmId { get; set; }

	        [Association(ThisKey ="FirmId", OtherKey ="Id")]
	        public FirmInfo FirmInfo { get; set; }

	        [Association(ExpressionPredicate = nameof(DocPrepareAssignmentExp))]
	        public Assignment DocPrepareAssignment { get; set; }

	        private static Expression<Func<Request, Assignment, bool>> DocPrepareAssignmentExp()
	            => (r, a) => a.TargetId == r.Id;
	    }

	    [Table(Name ="FirmInfo")]
	    class FirmInfo
	    {
	        [Column]public int Id { get; set; }

	        [Association(ThisKey =nameof(Id), OtherKey ="FirmId")]
	        public IEnumerable<Request> Requests { get; set; }
	    }

	    [Table(Name ="Assignments")]
	    class Assignment 
	    {
	        [PrimaryKey, Identity] public int Id { get; set; } // Int
	        [Column, NotNull] public Guid DirectionId { get; set; } 
	        [Column, Nullable] public int TargetId { get; set; } // varchar(50)
	        [Column, Nullable] public DateTime? DateRevoke { get; set; } // varchar(50)
		}

		[Test, Combinatorial]
		public void TestAssociation(
			[IncludeDataSources(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)] string context
		)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<Request>())
				using (db.CreateLocalTable<FirmInfo>())
				using (db.CreateLocalTable<Assignment>())
				{
		            db.Insert(new Request { Id = 1002, FirmId=1 });
		            db.Insert(new FirmInfo { Id = 1 });
		            db.Insert(new Assignment { Id = 1, TargetId=1, DirectionId= new Guid("c5c0a778-694e-49d1-b1a0-f8ef5569c673") });

		            var query =
		                db.GetTable<Request>()
		                .Where(r => r.Id == 1002)
		                .Select(r => r.FirmInfo)
		                .SelectMany(r => r.Requests)
		                .Select(r => new 
		                {
		                    Instance = r,
		                    Instance2 = r.DocPrepareAssignment,
		                });

		            var res = query.ToArray();

					var query2 =
                        db.GetTable<Request>()
					    .Where(r => (r.Id == 1002))
					    .Select(r => r.FirmInfo)
					    .SelectMany(r => r.Requests)
					    .Select(r => r.DocPrepareAssignment);
  
				    var res2 = query.ToArray();
				}
			}
		}
		
	}
}
