//------------------------------------------------------------------------------
// <copyright file="DataTableExtenstions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

// DataSetExtensions will be ported in Net Standard 2.1: https://github.com/dotnet/corefx/issues/19771

#if NETSTANDARD2_0
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Collections;

namespace System.Data
{

	/// <summary>
	/// This static class defines the DataTable extension methods.
	/// </summary>
	public static class DataTableExtensions
    {
        /// <summary>
        ///   This method returns a IEnumerable of Datarows.
        /// </summary>
        /// <param name="source">
        ///   The source DataTable to make enumerable.
        /// </param>
        /// <returns>
        ///   IEnumerable of datarows.
        /// </returns>
        public static EnumerableRowCollection<DataRow> AsEnumerable(this DataTable source)
        {
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}

			return new EnumerableRowCollection<DataRow>(source);
        }
    }

	/// <summary>
	/// Provides an entry point so that Cast operator call can be intercepted within an extension method.
	/// </summary>
	public abstract class EnumerableRowCollection : IEnumerable
	{
		internal abstract Type ElementType { get; }
		internal abstract DataTable Table { get; }

		internal EnumerableRowCollection()
		{
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return null;
		}
	}

	internal class SortExpressionBuilder<T> : IComparer<List<object>>
	{
		/**
         *  This class ensures multiple orderby/thenbys are handled correctly. Its semantics is as follows:
         *  
         * Query 1:
         * orderby a
         * thenby  b
         * orderby c
         * orderby d
         * thenby  e
         * 
         * is equivalent to:
         * 
         * Query 2:
         * orderby d
         * thenby  e
         * thenby  c
         * thenby  a
         * thenby  b
         * 
         **/

		//Selectors and comparers are mapped using the index in the list.
		//E.g: _comparers[i] is used with _selectors[i]

		LinkedList<Func<T, object>> _selectors = new LinkedList<Func<T, object>>();
		LinkedList<Comparison<object>> _comparers = new LinkedList<Comparison<object>>();

		LinkedListNode<Func<T, object>> _currentSelector = null;
		LinkedListNode<Comparison<object>> _currentComparer = null;


		/// <summary>
		/// Adds a sorting selector/comparer in the correct order
		/// </summary>
		internal void Add(Func<T, object> keySelector, Comparison<object> compare, bool isOrderBy)
		{
			Debug.Assert(keySelector != null);
			Debug.Assert(compare != null);
			//Inputs are assumed to be valid. The burden for ensuring it is on the caller.

			if (isOrderBy)
			{
				_currentSelector = _selectors.AddFirst(keySelector);
				_currentComparer = _comparers.AddFirst(compare);
			}
			else
			{
				//ThenBy can only be called after OrderBy
				Debug.Assert(_currentSelector != null);
				Debug.Assert(_currentComparer != null);

				_currentSelector = _selectors.AddAfter(_currentSelector, keySelector);
				_currentComparer = _comparers.AddAfter(_currentComparer, compare);
			}
		}

		/// <summary>
		/// Represents a Combined selector of all selectors added thusfar.
		/// </summary>
		/// <returns>List of 'objects returned by each selector'. This list is the combined-selector</returns>
		public List<object> Select(T row)
		{
			List<object> result = new List<object>();

			foreach (Func<T, object> selector in _selectors)
			{
				result.Add(selector(row));
			}

			return result;
		}



		/// <summary>
		/// Represents a Comparer (of IComparer) that compares two combined-selectors using
		/// provided comparers for each individual selector.
		/// Note: Comparison is done in the order it was Added.
		/// </summary>
		/// <returns>Comparison result of the combined Sort comparer expression</returns>
		public int Compare(List<object> a, List<object> b)
		{
			Debug.Assert(a.Count == Count);

			int i = 0;
			foreach (Comparison<object> compare in _comparers)
			{
				int result = compare(a[i], b[i]);

				if (result != 0)
				{
					return result;
				}
				i++;
			}

			return 0;
		}

		internal int Count
		{
			get
			{
				Debug.Assert(_selectors.Count == _comparers.Count); //weak now that we have two dimensions
				return _selectors.Count;
			}
		}

		/// <summary>
		/// Clones the SortexpressionBuilder and returns a new object 
		/// that points to same comparer and selectors (in the same order).
		/// </summary>
		/// <returns></returns>
		internal SortExpressionBuilder<T> Clone()
		{
			SortExpressionBuilder<T> builder = new SortExpressionBuilder<T>();

			foreach (Func<T, object> selector in _selectors)
			{
				if (selector == _currentSelector.Value)
				{
					builder._currentSelector = builder._selectors.AddLast(selector);
				}
				else
				{
					builder._selectors.AddLast(selector);
				}
			}

			foreach (Comparison<object> comparer in _comparers)
			{
				if (comparer == _currentComparer.Value)
				{
					builder._currentComparer = builder._comparers.AddLast(comparer);
				}
				else
				{
					builder._comparers.AddLast(comparer);
				}
			}

			return builder;
		}

		/// <summary>
		/// Clones the SortExpressinBuilder and casts to type TResult.
		/// </summary>
		internal SortExpressionBuilder<TResult> CloneCast<TResult>()
		{
			SortExpressionBuilder<TResult> builder = new SortExpressionBuilder<TResult>();

			foreach (Func<T, object> selector in _selectors)
			{
				if (selector == _currentSelector.Value)
				{
					builder._currentSelector = builder._selectors.AddLast(r => selector((T)(object)r));
				}
				else
				{
					builder._selectors.AddLast(r => selector((T)(object)r));
				}
			}


			foreach (Comparison<object> comparer in _comparers)
			{
				if (comparer == _currentComparer.Value)
				{
					builder._currentComparer = builder._comparers.AddLast(comparer);
				}
				else
				{
					builder._comparers.AddLast(comparer);
				}
			}

			return builder;
		}

	} //end SortExpressionBuilder<T>

	/// <summary>
	/// This class provides a wrapper for DataTables to allow for querying via LINQ.
	/// </summary>
	public class EnumerableRowCollection<TRow> : EnumerableRowCollection, IEnumerable<TRow>
	{
		private readonly DataTable _table;
		private readonly IEnumerable<TRow> _enumerableRows;
		private readonly List<Func<TRow, bool>> _listOfPredicates;

		// Stores list of sort expression in the order provided by user. E.g. order by, thenby, thenby descending..
		private readonly SortExpressionBuilder<TRow> _sortExpression;

		private readonly Func<TRow, TRow> _selector;

#region Properties

		internal override Type ElementType
		{
			get
			{
				return typeof(TRow);
			}

		}

		internal IEnumerable<TRow> EnumerableRows
		{
			get
			{
				return _enumerableRows;
			}
		}

		internal override DataTable Table
		{
			get
			{
				return _table;
			}
		}


#endregion Properties

#region Constructors

		/// <summary>
		/// This constructor is used when Select operator is called with output Type other than input row Type.
		/// Basically fail on GetLDV(), but other LINQ operators must work.
		/// </summary>
		internal EnumerableRowCollection(IEnumerable<TRow> enumerableRows, bool isDataViewable, DataTable table)
		{
			Debug.Assert(!isDataViewable || table != null, "isDataViewable bug table is null");

			_enumerableRows = enumerableRows;
			if (isDataViewable)
			{
				_table = table;
			}
			_listOfPredicates = new List<Func<TRow, bool>>();
			_sortExpression = new SortExpressionBuilder<TRow>();
		}

		/// <summary>
		/// Basic Constructor
		/// </summary>
		internal EnumerableRowCollection(DataTable table)
		{
			_table = table;
			_enumerableRows = table.Rows.Cast<TRow>();
			_listOfPredicates = new List<Func<TRow, bool>>();
			_sortExpression = new SortExpressionBuilder<TRow>();
		}

		/// <summary>
		/// Copy Constructor that sets the input IEnumerable as enumerableRows
		/// Used to maintain IEnumerable that has linq operators executed in the same order as the user
		/// </summary>
		internal EnumerableRowCollection(EnumerableRowCollection<TRow> source, IEnumerable<TRow> enumerableRows, Func<TRow, TRow> selector)
		{
			Debug.Assert(null != enumerableRows, "null enumerableRows");

			_enumerableRows = enumerableRows;
			_selector = selector;
			if (null != source)
			{
				if (null == source._selector)
				{
					_table = source._table;
				}
				_listOfPredicates = new List<Func<TRow, bool>>(source._listOfPredicates);
				_sortExpression = source._sortExpression.Clone(); //deep copy the List
			}
			else
			{
				_listOfPredicates = new List<Func<TRow, bool>>();
				_sortExpression = new SortExpressionBuilder<TRow>();
			}
		}

#endregion Constructors

#region PublicInterface
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		///  This method returns an strongly typed iterator
		///  for the underlying DataRow collection.
		/// </summary>
		/// <returns>
		///   A strongly typed iterator.
		/// </returns>
		public IEnumerator<TRow> GetEnumerator()
		{
			return _enumerableRows.GetEnumerator();
		}
#endregion PublicInterface

#region Add Single Filter/Sort Expression

		/// <summary>
		/// Used to add a filter predicate.
		/// A conjunction of all predicates are evaluated in LinqDataView
		/// </summary>
		internal void AddPredicate(Func<TRow, bool> pred)
		{
			Debug.Assert(pred != null);
			_listOfPredicates.Add(pred);
		}

		/// <summary>
		/// Adds a sort expression when Keyselector is provided but not Comparer
		/// </summary>
		internal void AddSortExpression<TKey>(Func<TRow, TKey> keySelector, bool isDescending, bool isOrderBy)
		{
			AddSortExpression<TKey>(keySelector, Comparer<TKey>.Default, isDescending, isOrderBy);
		}

		/// <summary>
		/// Adds a sort expression when Keyselector and Comparer are provided.
		/// </summary>
		internal void AddSortExpression<TKey>(
			Func<TRow, TKey> keySelector,
			IComparer<TKey> comparer,
			bool isDescending,
			bool isOrderBy)
		{
			if (keySelector == null)
				throw new ArgumentNullException("keySelector");
			if (comparer == null)
				throw new ArgumentNullException("comparer");

			_sortExpression.Add(
				delegate (TRow input)
				{
					return (object)keySelector(input);
				},
				delegate (object val1, object val2)
				{
					return (isDescending ? -1 : 1) * comparer.Compare((TKey)val1, (TKey)val2);
				},
				isOrderBy);
		}

#endregion Add Single Filter/Sort Expression

	}

	public static class DataRowExtensions
	{
		public static T Field<T>(this DataRow row, string columnName)
		{
			if (row == null)
			{
				throw new ArgumentNullException("row");
			}

			return UnboxT<T>.Unbox(row[columnName]);
		}

		private static class UnboxT<T>
		{
			internal static readonly Converter<object, T> Unbox = Create(typeof(T));

			private static Converter<object, T> Create(Type type)
			{
				if (type.IsValueType)
				{
					if (type.IsGenericType && !type.IsGenericTypeDefinition && (typeof(Nullable<>) == type.GetGenericTypeDefinition()))
					{
						return (Converter<object, T>)Delegate.CreateDelegate(
							typeof(Converter<object, T>),
							typeof(UnboxT<T>)
								.GetMethod("NullableField", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
								.MakeGenericMethod(type.GetGenericArguments()[0]));
					}
					return ValueField;
				}
				return ReferenceField;
			}

			private static T ReferenceField(object value)
			{
				return ((DBNull.Value == value) ? default(T) : (T)value);
			}

			private static T ValueField(object value)
			{
				if (DBNull.Value == value)
				{
					throw new InvalidCastException();
				}
				return (T)value;
			}

			private static Nullable<TElem> NullableField<TElem>(object value) where TElem : struct
			{
				if (DBNull.Value == value)
				{
					return default(Nullable<TElem>);
				}
				return new Nullable<TElem>((TElem)value);
			}
		}
	}
}
#endif