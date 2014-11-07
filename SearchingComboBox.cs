using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RealBookExtracter {
    /// <summary>
    /// This is a combo box that allows the user to type into the edit box,
    /// but searches for the first item matching what they have typed in, and
    /// prevents them entering anything not in the list.
    /// </summary>
    public class SearchingComboBox : System.Windows.Forms.ComboBox {

        private bool _allowNoSelection = true;

        private bool _acceptTabKey = false;

        private bool _readOnly = false;

        private object _savedItem;
        private bool _useSavedItem;

        public SearchingComboBox() {
            base.DropDownStyle = ComboBoxStyle.DropDown;
            base.Text = null;
            SetStyle(ControlStyles.EnableNotifyMessage, true);
            InputMustMatch = true;
        }

        // Prevent it appearing in the properties window.
        [Browsable(false)]
        public new ComboBoxStyle DropDownStyle {
            get { return ComboBoxStyle.DropDown; }
        }

        protected override void OnDropDownStyleChanged(EventArgs e) {
            base.DropDownStyle = ComboBoxStyle.DropDown;
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            if (_readOnly) {
                e.Handled = true;
            } else if (e.KeyCode == Keys.Delete) {
                if (Text.Length == SelectionLength && AllowNoSelection) {
                    Text = String.Empty;
                    base.SelectedItem = null;
                }
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// If true (the default) deleting the text in the edit box causes no item (null)
        /// to be selected, otherwise it retains any current selection.
        /// </summary>

        [Browsable(true),
        Category("Behavior"),
        Description("Controls whether deleting the text causes nothing to be selected"),
        DefaultValue(true)]
        public bool AllowNoSelection {
            get { return _allowNoSelection; }
            set { _allowNoSelection = value; }
        }

        /// <summary>
        /// If true, the control processes the Tab key in its own KeyDown/KeyUp events.
        /// </summary>

        [Browsable(true),
        Category("Behaviour"),
        Description("Whether the control accepts the Tab key (processes it itself)"),
        DefaultValue(false)]
        public bool AcceptTabKey {
            get { return _acceptTabKey; }
            set { _acceptTabKey = value; }
        }

        /// <summary>
        /// If true, the user cannot type into the edit box, or select from the drop down.
        /// </summary>

        [Browsable(true),
        Category("Behavior"),
        Description("Whether the control allows any user selection"),
        DefaultValue(false)]
        public bool ReadOnly {
            get { return _readOnly; }
            set { _readOnly = value; }
        }

        [Browsable(true),
        Category("Behavior"),
        Description("Whether any input has to match an item in the list"),
        DefaultValue(true)]
        public bool InputMustMatch { get; set; }

        protected override bool IsInputKey(Keys keyData) {
            if (keyData == Keys.Tab) return _acceptTabKey;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyPress(KeyPressEventArgs e) {
            /* Whatever happens, we have handled the key, and
             * and don't want the base class to do anything with it */
            e.Handled = true;
            if (_readOnly) {
                base.OnKeyPress(e);
                return;
            }
            // Find the text entered so far - everything that is not selected.
            string soFar = Text.Substring(0, SelectionStart);
            // Now modify that with the key entered.
            if (e.KeyChar == '\b')
                if (soFar.Length < 2)
                    soFar = String.Empty;
                else
                    soFar = soFar.Substring(0, soFar.Length - 1);
            else
                soFar += e.KeyChar;

            if (soFar.Length == 0 && !AllowNoSelection && base.SelectedItem != null) {
                Text = Items[0].ToString();
                SelectionStart = 0;
                SelectionLength = Text.Length;
                base.OnKeyPress(e);
                return;
            }

            if (soFar.Length == 0) {
                Text = String.Empty;
                base.SelectedItem = null;
                base.OnKeyPress(e);
                return;
            }

            int pos = this.FindString(soFar);
            if (InputMustMatch && pos < 0) {
                // It's not in the list so ignore the key press
                base.OnKeyPress(e);
                return;
            }

            // Note that the SelectedIndexChanged event gets raised by this automatically.
            if (!InputMustMatch && pos < 0) {
                Text = soFar;
            } else if (pos >= 0) {
                Text = Items[pos].ToString();
            }
            SelectionStart = soFar.Length;
            SelectionLength = Math.Max(Text.Length - soFar.Length, 0);
            base.OnKeyPress(e);
        }

        protected override void OnKeyUp(KeyEventArgs e) {
            if (_readOnly)
                e.Handled = true;
            base.OnKeyUp(e);
        }


        public override string Text {
            get {
                return base.Text;
            }
            set {
                // Make sure it can be set only to valid values
                if ((value == null || value.Length == 0) && !AllowNoSelection)
                    return;
                if (value != null && value.Length > 0) {
                    int pos = FindString(value);
                    if (InputMustMatch && (FindString(value) < 0 || Items[pos].ToString() != value))
                        return;
                }
                base.Text = value;
                SelectionStart = Text.Length;
            }
        }

        protected override void OnDropDown(EventArgs e) {
            base.OnDropDown(e);
            _savedItem = SelectedItem;
        }

        protected override void OnSelectedIndexChanged(EventArgs e) {
            if (_readOnly && _useSavedItem) {
                if (SelectedItem != _savedItem)
                    SelectedItem = _savedItem;
            } else
                // This shouldn't raise any events.
                base.OnSelectedIndexChanged(e);
        }

        public new object SelectedItem {
            get { return base.SelectedItem; }
            set {
                try {
                    _useSavedItem = false;
                    base.SelectedItem = value;
                } finally {
                    _useSavedItem = true;
                }
            }
        }
    }
}
