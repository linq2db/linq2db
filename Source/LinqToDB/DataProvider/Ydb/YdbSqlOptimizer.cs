using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// Оптимизатор SQL‑дерева для провайдера YDB (YQL‑диалект).
	/// Переписывает сложные <c>DELETE</c>/<c>UPDATE</c>, чтобы они
	/// соответствовали возможностям YDB, и подключает собственный
	/// преобразователь выражений.
	/// </summary>
	sealed class YdbSqlOptimizer : BasicSqlOptimizer
	{
		public YdbSqlOptimizer(SqlProviderFlags sqlProviderFlags)
			: base(sqlProviderFlags)
		{
		}

		/// <inheritdoc/>
		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			// Класс YdbSqlExpressionConvertVisitor уже реализован в провайдере.
			return new YdbSqlExpressionConvertVisitor(allowModify);
		}

		/// <inheritdoc/>
		public override SqlStatement TransformStatement(SqlStatement statement,
			DataOptions dataOptions,
			MappingSchema mappingSchema)
		{
			// Базовые оптимизации.
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			return statement.QueryType switch
			{
				QueryType.Delete => CorrectYdbDelete((SqlDeleteStatement)statement, dataOptions),
				QueryType.Update => CorrectYdbUpdate((SqlUpdateStatement)statement, dataOptions, mappingSchema),
				_ => statement
			};
		}

		#region DELETE

		/// <summary>
		/// Преобразует мульти‑табличные <c>DELETE</c> в форму
		/// <c>DELETE FROM … WHERE EXISTS(…)</c>, понятную YDB.
		/// </summary>
		SqlStatement CorrectYdbDelete(SqlDeleteStatement statement, DataOptions dataOptions)
		{
			// Используем готовый общий преобразователь из базового класса.
			return GetAlternativeDelete(statement, dataOptions);
		}

		#endregion

		#region UPDATE

		/// <summary>
		/// Переписывает сложные <c>UPDATE</c> (с JOIN или SELECT‑подзапросами)
		/// в ANSI‑совместимую форму, которую поддерживает YDB.
		/// </summary>
		SqlStatement CorrectYdbUpdate(SqlUpdateStatement statement,
			DataOptions dataOptions,
			MappingSchema mappingSchema)
		{
			// Универсальный алгоритм из BasicSqlOptimizer:
			//   • нормализует SET‑выражения;
			//   • отрывает таблицу‑назначение от JOIN‑ов;
			//   • формирует подзапрос‑фильтр EXISTS при необходимости.
			return GetAlternativeUpdate(statement, dataOptions, mappingSchema);
		}

		#endregion
	}
}
