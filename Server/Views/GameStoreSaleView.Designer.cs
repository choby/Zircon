namespace Server.Views
{
    partial class GameStoreSaleView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.GameStoreSaleGridControl = new DevExpress.XtraGrid.GridControl();
            this.GameStoreSaleGridView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.gridColumn1 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColumn5 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.AccountLookUpEdit = new DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit();
            this.gridColumn2 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ItemLookUpEdit = new DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit();
            this.gridColumn3 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColumn4 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColumn6 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColumn7 = new DevExpress.XtraGrid.Columns.GridColumn();
            ((System.ComponentModel.ISupportInitialize)(this.GameStoreSaleGridControl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GameStoreSaleGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.AccountLookUpEdit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ItemLookUpEdit)).BeginInit();
            this.SuspendLayout();
            // 
            // GameStoreSaleGridControl
            // 
            this.GameStoreSaleGridControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GameStoreSaleGridControl.Location = new System.Drawing.Point(0, 0);
            this.GameStoreSaleGridControl.MainView = this.GameStoreSaleGridView;
            this.GameStoreSaleGridControl.Name = "GameStoreSaleGridControl";
            this.GameStoreSaleGridControl.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.AccountLookUpEdit,
            this.ItemLookUpEdit});
            this.GameStoreSaleGridControl.Size = new System.Drawing.Size(937, 373);
            this.GameStoreSaleGridControl.TabIndex = 0;
            this.GameStoreSaleGridControl.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GameStoreSaleGridView});
            // 
            // GameStoreSaleGridView
            // 
            this.GameStoreSaleGridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.gridColumn1,
            this.gridColumn5,
            this.gridColumn2,
            this.gridColumn3,
            this.gridColumn4,
            this.gridColumn6,
            this.gridColumn7});
            this.GameStoreSaleGridView.GridControl = this.GameStoreSaleGridControl;
            this.GameStoreSaleGridView.Name = "GameStoreSaleGridView";
            this.GameStoreSaleGridView.OptionsView.EnableAppearanceEvenRow = true;
            this.GameStoreSaleGridView.OptionsView.EnableAppearanceOddRow = true;
            this.GameStoreSaleGridView.OptionsView.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;
            this.GameStoreSaleGridView.OptionsView.ShowGroupPanel = false;
            // 
            // gridColumn1
            // 
            this.gridColumn1.Caption = "索引";
            this.gridColumn1.FieldName = "Index";
            this.gridColumn1.Name = "gridColumn1";
            this.gridColumn1.Visible = true;
            this.gridColumn1.VisibleIndex = 0;
            // 
            // gridColumn5
            // 
            this.gridColumn5.ColumnEdit = this.AccountLookUpEdit;
            this.gridColumn5.Caption = "账号";
            this.gridColumn5.FieldName = "Account";
            this.gridColumn5.Name = "gridColumn5";
            this.gridColumn5.Visible = true;
            this.gridColumn5.VisibleIndex = 1;
            // 
            // AccountLookUpEdit
            // 
            this.AccountLookUpEdit.AutoHeight = false;
            this.AccountLookUpEdit.BestFitMode = DevExpress.XtraEditors.Controls.BestFitMode.BestFitResizePopup;
            this.AccountLookUpEdit.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.AccountLookUpEdit.Columns.AddRange(new DevExpress.XtraEditors.Controls.LookUpColumnInfo[] {
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Index", "索引"),
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo("EMailAddress", "电子邮箱地址"),
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Referral", "推荐人"),
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Banned", "已封禁")});
            this.AccountLookUpEdit.DisplayMember = "EMailAddress";
            this.AccountLookUpEdit.Name = "AccountLookUpEdit";
            this.AccountLookUpEdit.NullText = "";
            // 
            // gridColumn2
            // 
            this.gridColumn2.ColumnEdit = this.ItemLookUpEdit;
            this.gridColumn2.Caption = "物品";
            this.gridColumn2.FieldName = "Item";
            this.gridColumn2.Name = "gridColumn2";
            this.gridColumn2.Visible = true;
            this.gridColumn2.VisibleIndex = 2;
            // 
            // ItemLookUpEdit
            // 
            this.ItemLookUpEdit.AutoHeight = false;
            this.ItemLookUpEdit.BestFitMode = DevExpress.XtraEditors.Controls.BestFitMode.BestFitResizePopup;
            this.ItemLookUpEdit.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.ItemLookUpEdit.Columns.AddRange(new DevExpress.XtraEditors.Controls.LookUpColumnInfo[] {
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Index", "索引"),
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo("ItemName", "物品名称"),
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo("ItemType", "物品类型"),
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Price", "价格"),
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo("StackSize", "堆叠上限")});
            this.ItemLookUpEdit.DisplayMember = "ItemName";
            this.ItemLookUpEdit.Name = "ItemLookUpEdit";
            this.ItemLookUpEdit.NullText = "[物品为空]";
            // 
            // gridColumn3
            // 
            this.gridColumn3.DisplayFormat.FormatString = "F";
            this.gridColumn3.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.gridColumn3.Caption = "日期";
            this.gridColumn3.FieldName = "Date";
            this.gridColumn3.Name = "gridColumn3";
            this.gridColumn3.Visible = true;
            this.gridColumn3.VisibleIndex = 3;
            // 
            // gridColumn4
            // 
            this.gridColumn4.Caption = "价格";
            this.gridColumn4.FieldName = "Price";
            this.gridColumn4.Name = "gridColumn4";
            this.gridColumn4.Visible = true;
            this.gridColumn4.VisibleIndex = 4;
            // 
            // gridColumn6
            // 
            this.gridColumn6.Caption = "数量";
            this.gridColumn6.FieldName = "Count";
            this.gridColumn6.Name = "gridColumn6";
            this.gridColumn6.Visible = true;
            this.gridColumn6.VisibleIndex = 5;
            // 
            // gridColumn7
            // 
            this.gridColumn7.Caption = "狩猎金币";
            this.gridColumn7.FieldName = "HuntGold";
            this.gridColumn7.Name = "gridColumn7";
            this.gridColumn7.Visible = true;
            this.gridColumn7.VisibleIndex = 6;
            // 
            // GameStoreSaleView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(937, 373);
            this.Controls.Add(this.GameStoreSaleGridControl);
            this.Name = "GameStoreSaleView";
            this.Text = "商店销售";
            ((System.ComponentModel.ISupportInitialize)(this.GameStoreSaleGridControl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GameStoreSaleGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.AccountLookUpEdit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ItemLookUpEdit)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl GameStoreSaleGridControl;
        private DevExpress.XtraGrid.Views.Grid.GridView GameStoreSaleGridView;
        private DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit AccountLookUpEdit;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn1;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn5;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn2;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn3;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn4;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn6;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn7;
        private DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit ItemLookUpEdit;
    }
}
