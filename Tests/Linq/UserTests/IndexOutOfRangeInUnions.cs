using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class IndexOutOfRangeInUnions : TestBase
	{
		[Test]
		public void ErrorInUinon([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ClassTypeOne>("O1"))
			using (db.CreateLocalTable<ClassTypeOne>("O2"))
			using (db.CreateLocalTable<ClassTypeOne>("O3"))
			{
				IQueryable<ClassTypeOfResult>? query = null;

				foreach (var tipoDeDocumento in Enumerable.Range(1, 3))
				{
					var innerQuery =
						from doSap in db.GetTable<ClassTypeOne>()
							.TableName($"O{tipoDeDocumento}")
						select new ClassTypeOfResult
						{
							NumeroInterno   = doSap.DocEntry,
							StatusValor     = doSap.DocStatus == "O" ? "Aberto" : "Fechado",
							DescricaoStatus = "Manual/Externo",
						};

					query = query?.Union(innerQuery) ?? innerQuery;
				}

				query!.ToList();
			}
		}

		public class ClassTypeOne
		{
			public int     DocEntry    { get; set; }
			public int     BplId       { get; set; }
			public string? ChaveAcesso { get; set; }
			public string? DocStatus   { get; set; }
		}

		public class ClassTypeOfResult
		{
			public int     NumeroInterno   { get; set; }
			public string? ChaveDeAcesso   { get; set; }
			public string? StatusValor     { get; set; }
			public string? DescricaoStatus { get; set; }
		}
	}
}
