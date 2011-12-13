using System;
using System.Collections.Generic;
using System.Diagnostics;
using LinqToDB.Common;
using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	public class DictionaryListMapper<K,T> : IMapDataDestinationList
	{
		public DictionaryListMapper(
			IDictionary<K,T>     dic,
			NameOrIndexParameter keyField,
			ObjectMapper         objectMapper)
		{
			_dic        = dic;
			_mapper     = objectMapper;
			_keyGetter  = MapGetData<K>.I;
			_fromSource = keyField.ByName && keyField.Name[0] == '@';
			_keyField   = _fromSource? keyField.Name.Substring(1): keyField;
		}

		private readonly IDictionary<K,T>     _dic;
		private readonly bool                 _fromSource;
		private readonly MapGetData<K>.MB<K>  _keyGetter;
		private          NameOrIndexParameter _keyField;
		private          int                  _index;
		private          ObjectMapper         _mapper;
		private          object               _newObject;
		private          bool                 _typeMismatch;
		private          K                    _keyValue;

		#region IMapDataDestinationList Members

		private void AddObject()
		{
			if (_newObject != null)
			{
				if (_typeMismatch)
					_keyValue = _mapper.MappingSchema.ConvertTo<K,object>(_mapper[_index].GetValue(_newObject));
				else if (!_fromSource)
					_keyValue = _keyGetter.From(_mapper, _newObject, _index);

				_dic[_keyValue] = (T)_newObject;
			}
		}

		public virtual void InitMapping(InitContext initContext)
		{
			var sm = _dic as ISupportMapping;

			if (sm != null)
			{
				sm.BeginMapping(initContext);

				if (_mapper != initContext.ObjectMapper)
					_mapper = initContext.ObjectMapper;
			}

			if (_fromSource)
				_index = _keyField.ByName? initContext.DataSource.GetOrdinal(_keyField.Name): _keyField.Index;
			else
			{
				_index = _keyField.ByName? _mapper.GetOrdinal(_keyField.Name, true): _keyField.Index;

				if (_index < 0)
					throw new MappingException(
						_keyField.ByName?
						string.Format("Field '{0}' not found.", _keyField.Name):
						string.Format("Index '{0}' is invalid.", _keyField.Index));

				var mm = _mapper[_index];
				_typeMismatch = !TypeHelper.IsSameOrParent(typeof(K), mm.Type);

#if !SILVERLIGHT

				Debug.WriteLineIf(_typeMismatch, string.Format(
					"Member {0} type '{1}' does not match dictionary key type '{2}'.",
						mm.Name, mm.Type.Name, (typeof(K).Name)));

#endif
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
				_keyValue = _keyGetter.From(initContext.DataSource, initContext.SourceObject, _index);

				if (Common.Configuration.TrimDictionaryKey && _keyValue is string)
					_keyValue = (K)(object)_keyValue.ToString().TrimEnd(_trim);
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
