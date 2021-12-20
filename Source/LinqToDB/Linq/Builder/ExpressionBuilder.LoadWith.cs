using System;
using System.Collections.Generic;
using LinqToDB.Common;

namespace LinqToDB.Linq.Builder
{
	partial class ExpressionBuilder
	{
		private Dictionary<IBuildContext, List<LoadWithInfo[]>>? _loadWithInformation;

		public List<LoadWithInfo[]>? GetLoadWith(IBuildContext sequence)
		{
			if (_loadWithInformation == null || !_loadWithInformation.TryGetValue(sequence, out var loadWithInfo))
				return null;

			return loadWithInfo;
		}

		public void RegisterLoadWith(IBuildContext sequence, LoadWithInfo[] loadWithInfo, bool appendToLast)
		{
			_loadWithInformation ??= new();

			if (!_loadWithInformation.TryGetValue(sequence, out var current))
			{
				current = new List<LoadWithInfo[]>();

				_loadWithInformation[sequence] = current;
			}

			if (appendToLast)
			{
				if (current.Count == 0)
					throw new InvalidOperationException();

				current[current.Count - 1] = Array<LoadWithInfo>.Append(current[current.Count - 1], loadWithInfo);
			}
			else
			{
				current.Add(loadWithInfo);
			}
		}
	}
}
