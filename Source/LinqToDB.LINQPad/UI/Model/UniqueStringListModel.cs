using System.Collections.ObjectModel;

namespace LinqToDB.LINQPad.UI;

internal sealed class UniqueStringListModel
{
	public string?                      Title   { get; set; }
	public string?                      ToolTip { get; set; }
	public bool                         Include { get; set; }
	public ObservableCollection<string> Items   { get;      } = new();
}
