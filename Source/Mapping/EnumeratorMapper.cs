using System;
using System.Collections;

using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	public class EnumeratorMapper : IMapDataSourceList
	{
		public EnumeratorMapper(IEnumerator enumerator)
		{
			_enumerator = enumerator;
		}

		private readonly IEnumerator _enumerator;
		private          Type        _objectType;

		#region IMapDataSourceList Members

		public virtual void InitMapping(InitContext initContext)
		{
			_enumerator.Reset();
		}

		public virtual bool SetNextDataSource(InitContext initContext)
		{
			if (initContext == null) throw new ArgumentNullException("initContext");

			if (_enumerator.MoveNext() == false)
				return false;

			object sourceObject = _enumerator.Current;

			if (_objectType != sourceObject.GetType())
			{
				_objectType = sourceObject.GetType();
				initContext.DataSource = initContext.MappingSchema.GetObjectMapper(_objectType);
			}

			initContext.SourceObject = sourceObject;

			return true;
		}

		public virtual void EndMapping(InitContext initContext)
		{
		}

		#endregion
	}
}
