
// This file has been generated by the GUI designer. Do not modify.
namespace Nexus
{
	public partial class DialogEditFixedTiles
	{
		private global::Gtk.Button button604;

		private global::Gtk.Button buttonOk;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget Nexus.DialogEditFixedTiles
			this.Name = "Nexus.DialogEditFixedTiles";
			this.Title = global::Mono.Unix.Catalog.GetString ("Edit Fixed Tiles");
			this.Icon = global::Stetic.IconLoader.LoadIcon (this, "gtk-edit", global::Gtk.IconSize.Dialog);
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.Modal = true;
			// Internal child Nexus.DialogEditFixedTiles.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Internal child Nexus.DialogEditFixedTiles.ActionArea
			global::Gtk.HButtonBox w2 = this.ActionArea;
			w2.Name = "dialog1_ActionArea";
			w2.Spacing = 6;
			w2.BorderWidth = ((uint)(5));
			w2.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button604 = new global::Gtk.Button ();
			this.button604.CanFocus = true;
			this.button604.Name = "button604";
			this.button604.UseStock = true;
			this.button604.UseUnderline = true;
			this.button604.Label = "gtk-cancel";
			this.AddActionWidget (this.button604, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w3 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w2[this.button604]));
			w3.Expand = false;
			w3.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button ();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget (this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w4 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w2[this.buttonOk]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.DefaultWidth = 312;
			this.DefaultHeight = 300;
			this.Show ();
		}
	}
}
