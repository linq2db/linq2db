using System;
using System.Collections;

using LinqToDB.Common;
using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	public class DictionaryListMapper : IMapDataDestinationList
	{
		public DictionaryListMapper(
			IDictionary          dic,
			NameOrIndexParameter keyField,
			ObjectMapper         objectMapper)
		{
			_dic        = dic;
			_mapper     = objectMapper;
			_fromSource = keyField.ByName && keyField.Name[0] == '@';
			_keyField   = _fromSource? keyField.Name.Substring(1): keyField;
		}

		private readonly IDictionary          _dic;
		private readonly bool                 _fromSource;
		private          NameOrIndexParameter _keyField;
		private          ObjectMapper         _mapper;
		private          object               _newObject;
		private          object               _keyValue;

		#region IMapDataDestinationList Members

		private void AddObject()
		{
			if (_newObject != null)
			{
				if (!_fromSource)
					_keyValue = _mapper.TypeAccessor[_keyField].GetValue(_newObject);

				_dic[_keyValue]  = _newObject;
			}
		}

		public virtual void InitMapping(InitContext initContext)
		{
			ISupportMapping sm = _dic as ISupportMapping;

			if (sm != null)
			{
				sm.BeginMapping(initContext);

				if (_mapper != initContext.ObjectMapper)
					_mapper = initContext.ObjectMapper;
			}
		}

		[CLSCompliant(false)]
		public virtual IMapDataDestination GetDataDestination(InitContext initContext)
		{
			return _mapper;
		}

		static readonly char[] _trim = { ' ' };

		public virtual object GetNextObject(InitContext initContext)
		{
			AddObject();

			if (_fromSource)
			{
				_keyValue = _keyField.ByName ? 
					initContext.DataSource.GetValue(initContext.SourceObject, _keyField.Name) :
					initContext.DataSource.GetValue(initContext.SourceObject, _keyField.Index);

				if (Common.Configuration.TrimDictionaryKey && _keyValue is string)
					_keyValue = _keyValue.ToString().TrimEnd(_trim);
			}

			return _newObject = _mapper.CreateInstance(initContext);
		}

		public virtual void EndMapping(InitContext initContext)
		{
			AddObject();

			ISupportMapping sm = _dic as ISupportMapping;

			if (sm != null)
				sm.EndMapping(initContext);
		}

		#endregion
	}
}
