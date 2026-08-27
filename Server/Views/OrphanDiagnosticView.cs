using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Server.Diagnostics;
using Server.Envir;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Server.Views
{
    public partial class OrphanDiagnosticView : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        private readonly BindingList<OrphanDiagnostic.OrphanTypeResult> _results = new BindingList<OrphanDiagnostic.OrphanTypeResult>();

        public OrphanDiagnosticView()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            DiagnosticGridView.OptionsSelection.MultiSelect = true;
            DiagnosticGridView.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CellSelect;
            DiagnosticGridControl.DataSource = _results;
            DiagnosticGridView.OptionsBehavior.Editable = false;
            DiagnosticGridView.OptionsBehavior.ReadOnly = true;
        }

        private void ScanOrphansButton_ItemClick(object sender, ItemClickEventArgs e)
        {
            RunScan(cleanRun: false);
        }

        private void CleanOrphansButton_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show(this,
                "此操作会将所有可清理的聚合子项孤立数据标记为临时数据，使其在下次保存数据库时被跳过。是否继续？",
                "清理数据库孤立数据",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            RunScan(cleanRun: true);
        }

        private void RunScan(bool cleanRun)
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                OrphanDiagnostic.ScanResult result = cleanRun
                    ? OrphanDiagnostic.MarkTemporaryOnCleanableOrphans()
                    : OrphanDiagnostic.Scan();

                _results.Clear();
                foreach (OrphanDiagnostic.OrphanTypeResult row in result.Results)
                    _results.Add(row);

                string log = OrphanDiagnostic.FormatLog(result, cleanRun);
                memoEdit1.EditValue = log;

                DiagnosticGridView.BestFitColumns();
            }
            catch (Exception ex)
            {
                string message = cleanRun ? "清理数据库孤立数据失败：" + ex.Message : "扫描数据库孤立数据失败：" + ex.Message;
                memoEdit1.EditValue = message;
                SEnvir.Log(message);
                XtraMessageBox.Show(this, message, "数据库孤立数据", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}
