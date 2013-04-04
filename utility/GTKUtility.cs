using System;
using Gtk;

namespace Nexus
{
	public static class GTKUtility
	{
		public static void SetComboBoxValue(ComboBox comboBox, string text)
		{
			comboBox.Model.Foreach(delegate(TreeModel model, TreePath path, TreeIter iter)
			{
				if (string.Equals(text, (string)model.GetValue(iter, 0)))
				{
					comboBox.SetActiveIter(iter);
					return (true);
				}

				return (false);
			});
		}
	}
}
