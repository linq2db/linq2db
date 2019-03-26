using System;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.Linq.Relinq.Visitors;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using Remotion.Linq;

namespace LinqToDB.Linq.Relinq
{
	public partial class RelinqQueryGenerator
	{
		public class GenrationResult
		{
			public GenrationResult(QueryModel queryModel, Expression projection, SqlStatement statement)
			{
				QueryModel = queryModel;
				Projection = projection;
				Statement = statement;
			}

			public QueryModel   QueryModel { get; }
			public Expression   Projection { get; }
			public SqlStatement Statement { get; }
		}

		public IDataContext DataContext { get; }
		public MappingSchema MappingSchema => DataContext.MappingSchema;

		public RelinqQueryGenerator([JetBrains.Annotations.NotNull] IDataContext dataContext)
		{
			DataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
		}

		public GenrationResult GenerateQuery(Expression queryExpression)
		{
			var parsingContext = new ParsingContext(DataContext);

			var queryModel = parsingContext.ParseModel(queryExpression);

			var associationsMapper = new ExpressionMapper();
			var associationResolverVisitor = new AssociationResolverVisitor(parsingContext, DataContext, associationsMapper);
			associationResolverVisitor.VisitQueryModel(queryModel);
		
			var generateVisitor = new QueryGenerationVisitor(DataContext, associationResolverVisitor);

			generateVisitor.VisitQueryModel(queryModel);
			var projection = generateVisitor.GenerateProjection(queryModel.SelectClause.Selector, (e, s) =>
			{
				var idx = generateVisitor.CurrentQuery.Select.Add(s);
				return new ConvertFromDataReaderExpression(e.Type, idx, ExpressionPredefines.DataReaderParam,
					DataContext);
			}, false);

			return new GenrationResult(queryModel, projection, new SqlSelectStatement(generateVisitor.CurrentQuery));
		}

	}
}
