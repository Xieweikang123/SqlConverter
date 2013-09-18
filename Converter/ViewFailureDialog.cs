using System;
using System.Windows.Forms;
using DbAccess;

namespace Converter.WinForms
{
    public partial class ViewFailureDialog : Form
    {
        public ViewFailureDialog()
        {
            this.InitializeComponent();
        }

        public ViewSchema View
        {
            get { return this._view; }
            set
            {
                this._view = value;
                this.Text = "SQL Error: "+this._view.ViewName;
                this.txtSQL.Text = this._view.ViewSQL;
            }
        }

        public string ViewSQL
        {
            get { return this.txtSQL.Text; }
        }


        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private ViewSchema _view;
    }
}