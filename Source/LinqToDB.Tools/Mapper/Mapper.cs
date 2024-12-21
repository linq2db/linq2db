using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Common;

namespace LinqToDB.Tools.Mapper
{
	/// <summary>
	/// Maps an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
	/// </summary>
	/// <typeparam name="TFrom">Type to map from.</typeparam>
	/// <typeparam name="TTo">Type to map to.</typeparam>
	/// <example>
	/// This <see href="https://github.com/rsdn/CodeJam/blob/master/CodeJam.Blocks.Tests/Mapping/Examples/MapTests.cs">example</see> shows how to map one object to another.
	/// </example>
	[PublicAPI]
	public class Mapper<TFrom,TTo>
	{
		MapperBuilder<TFrom,TTo>                                     _mapperBuilder;
		Expression<Func<TFrom,TTo,IDictionary<object,object>?,TTo>>? _mapperExpression;
		Expression<Func<TFrom,TTo>>?                                 _mapperExpressionEx;
		Func<TFrom,TTo,IDictionary<object,object>?,TTo>?             _mapperEx;
		Func<TFrom,TTo>?                                             _mapper;

		internal Mapper(MapperBuilder<TFrom,TTo> mapperBuilder) => _mapperBuilder = mapperBuilder;

		/// <summary>
		/// Returns a mapper expression to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// Returned expression is compatible to IQueryable.
		/// </summary>
		/// <returns>Mapping expression.</returns>
		[Pure]
		public Expression<Func<TFrom,TTo>> GetMapperExpression()
			=> _mapperExpressionEx ??= _mapperBuilder.GetMapperExpression();

		/// <summary>
		/// Returns a mapper expression to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <returns>Mapping expression.</returns>
		[Pure]
		public Expression<Func<TFrom,TTo,IDictionary<object,object>?,TTo>> GetMapperExpressionEx()
			=> _mapperExpression ??= _mapperBuilder.GetMapperExpressionEx();

		/// <summary>
		/// Returns a mapper to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <returns>Mapping expression.</returns>
		[Pure]
		public Func<TFrom,TTo> GetMapper()
			=> _mapper ??= GetMapperExpression().CompileExpression();

		/// <summary>
		/// Returns a mapper to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <returns>Mapping expression.</returns>
		[Pure]
		public Func<TFrom,TTo,IDictionary<object,object>?,TTo> GetMapperEx()
			=> _mapperEx ??= GetMapperExpressionEx().CompileExpression();

		/// <summary>
		/// Returns a mapper to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <param name="source">Object to map.</param>
		/// <returns>Destination object.</returns>
		[Pure]
		public TTo Map(TFrom source)
			=> GetMapper()(source);

		/// <summary>
		/// Returns a mapper to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <param name="source">Object to map.</param>
		/// <param name="destination">Destination object.</param>
		/// <returns>Destination object.</returns>
		public TTo Map(TFrom source, TTo destination)
			=> GetMapperEx()(source, destination, new Dictionary<object,object>());

		/// <summary>
		/// Returns a mapper to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <param name="source">Object to map.</param>
		/// <param name="destination">Destination object.</param>
		/// <param name="crossReferenceDictionary">Storage for cress references if applied.</param>
		/// <returns>Destination object.</returns>
		[Pure]
		public TTo Map(TFrom source, TTo destination, IDictionary<object,object>? crossReferenceDictionary)
			=> GetMapperEx()(source, destination, crossReferenceDictionary);
	}
}
