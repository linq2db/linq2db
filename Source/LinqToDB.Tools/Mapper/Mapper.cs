﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Tools.Mapper
{
	/// <summary>
	/// Maps an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
	/// </summary>
	/// <typeparam name="TFrom">Type to map from.</typeparam>
	/// <typeparam name="TTo">Type to map to.</typeparam>
	/// <example>
	/// This example shows how to map one object to another.
	/// <code source="CodeJam.Blocks.Tests\Mapping\Examples\MapTests.cs" region="Example" lang="C#"/>
	/// </example>
	[PublicAPI]
	public class Mapper<TFrom,TTo>
	{
		[NotNull]
		MapperBuilder<TFrom,TTo>                                   _mapperBuilder;
		[CanBeNull]
		Expression<Func<TFrom,TTo,IDictionary<object,object>,TTo>> _mapperExpression;
		Expression<Func<TFrom,TTo>>                                _mapperExpressionEx;
		Func<TFrom,TTo,IDictionary<object,object>,TTo>             _mapperEx;
		Func<TFrom,TTo>                                            _mapper;

		internal Mapper([NotNull] MapperBuilder<TFrom,TTo> mapperBuilder) => _mapperBuilder = mapperBuilder;

		/// <summary>
		/// Returns a mapper expression to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// Returned expression is compatible to IQueryable.
		/// </summary>
		/// <returns>Mapping expression.</returns>
		[Pure, NotNull]
		public Expression<Func<TFrom,TTo>> GetMapperExpression()
			=> _mapperExpressionEx ?? (_mapperExpressionEx = _mapperBuilder.GetMapperExpression());

		/// <summary>
		/// Returns a mapper expression to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <returns>Mapping expression.</returns>
		[Pure, NotNull]
		public Expression<Func<TFrom,TTo,IDictionary<object,object>,TTo>> GetMapperExpressionEx()
			=> _mapperExpression ?? (_mapperExpression = _mapperBuilder.GetMapperExpressionEx());

		/// <summary>
		/// Returns a mapper to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <returns>Mapping expression.</returns>
		[Pure, NotNull]
		public Func<TFrom,TTo> GetMapper()
			=> _mapper ?? (_mapper = GetMapperExpression().Compile());

		/// <summary>
		/// Returns a mapper to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <returns>Mapping expression.</returns>
		[Pure, NotNull]
		public Func<TFrom,TTo,IDictionary<object,object>,TTo> GetMapperEx()
			=> _mapperEx ?? (_mapperEx = GetMapperExpressionEx().Compile());

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
		public TTo Map(TFrom source, TTo destination, IDictionary<object,object> crossReferenceDictionary)
			=> GetMapperEx()(source, destination, crossReferenceDictionary);
	}
}
