using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.Tools.Activity
{
	public class ActivityHierarchy : ActivityBase
	{
		readonly ActivityID              _activityID;
		readonly string                  _name;
		readonly Action<string>          _pushReport;
		readonly ActivityHierarchy?      _parent;
		readonly List<ActivityHierarchy> _children = [];

		int _count = 1;

		public ActivityHierarchy(ActivityID activityID, Action<string> pushReport)
		{
			_activityID = activityID;
			_pushReport = pushReport;
			_parent     = Current;
			_name       = ActivityStatistics.GetStat(activityID).Name.TrimStart();

			Current = this;

			if (_parent != null)
			{
				if (_parent._children.Count == 0)
				{
					_parent._children.Add(this);
				}
				else
				{
					var p = _parent._children[^1];

					if (p._activityID == _activityID)
						p._count++;
					else
						_parent._children.Add(this);
				}
			}
			else if (activityID is not (ActivityID.QueryProviderExecuteT or ActivityID.QueryProviderGetEnumeratorT))
			{
			}
		}

		static readonly System.Threading.AsyncLocal<ActivityHierarchy?> _currentImpl = new ();

		static ActivityHierarchy? Current
		{
			get => _currentImpl.Value;
			set => _currentImpl.Value = value;
		}

		public override void Dispose()
		{
			Current = _parent;

			if (_parent == null)
			{
				var sb = new StringBuilder();

				Print(this, 0);

				_pushReport(sb.ToString());

				void Print(ActivityHierarchy a, int indent)
				{
					sb
						.Append(' ', indent)
						.Append(a._name);

					if (a._count > 1)
						sb.Append($" ({a._count})");

					sb.AppendLine();

					foreach (var c in a._children)
						Print(c, indent + 2);
				}
			}
		}
	}
}
