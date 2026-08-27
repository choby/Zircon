namespace Server.Views
{
    partial class CastleInfoView
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
            DevExpress.XtraGrid.GridLevelNode gridLevelNode1 = new DevExpress.XtraGrid.GridLevelNode();
            DevExpress.XtraGrid.GridLevelNode gridLevelNode2 = new DevExpress.XtraGrid.GridLevelNode();
            DevExpress.XtraGrid.GridLevelNode gridLevelNode3 = new DevExpress.XtraGrid.GridLevelNode();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CastleInfoView));
            FlagGridView = new DevExpress.XtraGrid.Views.Grid.GridView();
            gridColumn10 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn11 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn12 = new DevExpress.XtraGrid.Columns.GridColumn();
            MonsterLookUpEdit = new DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit();
            CastleInfoGridControl = new DevExpress.XtraGrid.GridControl();
            GateGridView = new DevExpress.XtraGrid.Views.Grid.GridView();
            gridColumn13 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn14 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn15 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn20 = new DevExpress.XtraGrid.Columns.GridColumn();
            GuardGridView = new DevExpress.XtraGrid.Views.Grid.GridView();
            gridColumn16 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn17 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn18 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn19 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn21 = new DevExpress.XtraGrid.Columns.GridColumn();
            CastleInfoGridView = new DevExpress.XtraGrid.Views.Grid.GridView();
            gridColumn1 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn2 = new DevExpress.XtraGrid.Columns.GridColumn();
            MapLookUpEdit = new DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit();
            gridColumn3 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn4 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn5 = new DevExpress.XtraGrid.Columns.GridColumn();
            RegionLookUpEdit = new DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit();
            gridColumn6 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn9 = new DevExpress.XtraGrid.Columns.GridColumn();
            ItemLookUpEdit = new DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit();
            gridColumn7 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn8 = new DevExpress.XtraGrid.Columns.GridColumn();
            ribbon = new DevExpress.XtraBars.Ribbon.RibbonControl();
            SaveButton = new DevExpress.XtraBars.BarButtonItem();
            ImportButton = new DevExpress.XtraBars.BarButtonItem();
            ExportButton = new DevExpress.XtraBars.BarButtonItem();
            ribbonPage1 = new DevExpress.XtraBars.Ribbon.RibbonPage();
            ribbonPageGroup1 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            JsonImportExport = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            repositoryItemTextEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            gridColumn22 = new DevExpress.XtraGrid.Columns.GridColumn();
            ((System.ComponentModel.ISupportInitialize)FlagGridView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)MonsterLookUpEdit).BeginInit();
            ((System.ComponentModel.ISupportInitialize)CastleInfoGridControl).BeginInit();
            ((System.ComponentModel.ISupportInitialize)GateGridView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)GuardGridView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)CastleInfoGridView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)MapLookUpEdit).BeginInit();
            ((System.ComponentModel.ISupportInitialize)RegionLookUpEdit).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ItemLookUpEdit).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ribbon).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemTextEdit1).BeginInit();
            SuspendLayout();
            // 
            // FlagGridView
            // 
            FlagGridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { gridColumn10, gridColumn11, gridColumn12 });
            FlagGridView.GridControl = CastleInfoGridControl;
            FlagGridView.Name = "FlagGridView";
            FlagGridView.OptionsView.EnableAppearanceEvenRow = true;
            FlagGridView.OptionsView.EnableAppearanceOddRow = true;
            FlagGridView.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Top;
            FlagGridView.OptionsView.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;
            FlagGridView.OptionsView.ShowGroupPanel = false;
            // 
            // gridColumn10
            // 
            gridColumn10.Caption = "Y";
            gridColumn10.FieldName = "Y";
            gridColumn10.Name = "gridColumn10";
            gridColumn10.Visible = true;
            gridColumn10.VisibleIndex = 2;
            // 
            // gridColumn11
            // 
            gridColumn11.Caption = "X";
            gridColumn11.FieldName = "X";
            gridColumn11.Name = "gridColumn11";
            gridColumn11.Visible = true;
            gridColumn11.VisibleIndex = 1;
            // 
            // gridColumn12
            // 
            gridColumn12.Caption = "怪物";
            gridColumn12.ColumnEdit = MonsterLookUpEdit;
            gridColumn12.FieldName = "Monster";
            gridColumn12.Name = "gridColumn12";
            gridColumn12.Visible = true;
            gridColumn12.VisibleIndex = 0;
            // 
            // MonsterLookUpEdit
            // 
            MonsterLookUpEdit.AutoHeight = false;
            MonsterLookUpEdit.BestFitMode = DevExpress.XtraEditors.Controls.BestFitMode.BestFitResizePopup;
            MonsterLookUpEdit.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            MonsterLookUpEdit.Columns.AddRange(new DevExpress.XtraEditors.Controls.LookUpColumnInfo[] { new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Index", "索引"), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("MonsterName", "怪物名称"), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("AI", "AI"), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Level", "等级"), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Experience", "经验"), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("IsBoss", "是否首领") });
            MonsterLookUpEdit.DisplayMember = "MonsterName";
            MonsterLookUpEdit.Name = "MonsterLookUpEdit";
            MonsterLookUpEdit.NullText = "[怪物为空]";
            // 
            // CastleInfoGridControl
            // 
            CastleInfoGridControl.Dock = System.Windows.Forms.DockStyle.Fill;
            gridLevelNode1.LevelTemplate = FlagGridView;
            gridLevelNode1.RelationName = "Flags";
            gridLevelNode2.LevelTemplate = GateGridView;
            gridLevelNode2.RelationName = "Gates";
            gridLevelNode3.LevelTemplate = GuardGridView;
            gridLevelNode3.RelationName = "Guards";
            CastleInfoGridControl.LevelTree.Nodes.AddRange(new DevExpress.XtraGrid.GridLevelNode[] { gridLevelNode1, gridLevelNode2, gridLevelNode3 });
            CastleInfoGridControl.Location = new System.Drawing.Point(0, 144);
            CastleInfoGridControl.MainView = CastleInfoGridView;
            CastleInfoGridControl.MenuManager = ribbon;
            CastleInfoGridControl.Name = "CastleInfoGridControl";
            CastleInfoGridControl.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] { MonsterLookUpEdit, RegionLookUpEdit, MapLookUpEdit, repositoryItemTextEdit1, ItemLookUpEdit });
            CastleInfoGridControl.ShowOnlyPredefinedDetails = true;
            CastleInfoGridControl.Size = new System.Drawing.Size(728, 373);
            CastleInfoGridControl.TabIndex = 2;
            CastleInfoGridControl.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { GateGridView, GuardGridView, CastleInfoGridView, FlagGridView });
            // 
            // GateGridView
            // 
            GateGridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { gridColumn13, gridColumn14, gridColumn15, gridColumn20 });
            GateGridView.GridControl = CastleInfoGridControl;
            GateGridView.Name = "GateGridView";
            GateGridView.OptionsView.EnableAppearanceEvenRow = true;
            GateGridView.OptionsView.EnableAppearanceOddRow = true;
            GateGridView.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Top;
            GateGridView.OptionsView.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;
            GateGridView.OptionsView.ShowGroupPanel = false;
            // 
            // gridColumn13
            // 
            gridColumn13.Caption = "Y 坐标";
            gridColumn13.FieldName = "Y";
            gridColumn13.Name = "gridColumn13";
            gridColumn13.Visible = true;
            gridColumn13.VisibleIndex = 2;
            // 
            // gridColumn14
            // 
            gridColumn14.Caption = "X 坐标";
            gridColumn14.FieldName = "X";
            gridColumn14.Name = "gridColumn14";
            gridColumn14.Visible = true;
            gridColumn14.VisibleIndex = 1;
            // 
            // gridColumn15
            // 
            gridColumn15.ColumnEdit = MonsterLookUpEdit;
            gridColumn15.Caption = "怪物";
            gridColumn15.FieldName = "Monster";
            gridColumn15.Name = "gridColumn15";
            gridColumn15.Visible = true;
            gridColumn15.VisibleIndex = 0;
            // 
            // gridColumn20
            // 
            gridColumn20.Caption = "修理费用";
            gridColumn20.FieldName = "RepairCost";
            gridColumn20.Name = "gridColumn20";
            gridColumn20.Visible = true;
            gridColumn20.VisibleIndex = 3;
            // 
            // GuardGridView
            // 
            GuardGridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { gridColumn16, gridColumn17, gridColumn18, gridColumn19, gridColumn21 });
            GuardGridView.GridControl = CastleInfoGridControl;
            GuardGridView.Name = "GuardGridView";
            GuardGridView.OptionsView.EnableAppearanceEvenRow = true;
            GuardGridView.OptionsView.EnableAppearanceOddRow = true;
            GuardGridView.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Top;
            GuardGridView.OptionsView.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;
            GuardGridView.OptionsView.ShowGroupPanel = false;
            // 
            // gridColumn16
            // 
            gridColumn16.Caption = "方向";
            gridColumn16.FieldName = "Direction";
            gridColumn16.Name = "gridColumn16";
            gridColumn16.Visible = true;
            gridColumn16.VisibleIndex = 3;
            // 
            // gridColumn17
            // 
            gridColumn17.Caption = "Y 坐标";
            gridColumn17.FieldName = "Y";
            gridColumn17.Name = "gridColumn17";
            gridColumn17.Visible = true;
            gridColumn17.VisibleIndex = 2;
            // 
            // gridColumn18
            // 
            gridColumn18.Caption = "X 坐标";
            gridColumn18.FieldName = "X";
            gridColumn18.Name = "gridColumn18";
            gridColumn18.Visible = true;
            gridColumn18.VisibleIndex = 1;
            // 
            // gridColumn19
            // 
            gridColumn19.ColumnEdit = MonsterLookUpEdit;
            gridColumn19.Caption = "怪物";
            gridColumn19.FieldName = "Monster";
            gridColumn19.Name = "gridColumn19";
            gridColumn19.Visible = true;
            gridColumn19.VisibleIndex = 0;
            // 
            // gridColumn21
            // 
            gridColumn21.Caption = "修理费用";
            gridColumn21.FieldName = "RepairCost";
            gridColumn21.Name = "gridColumn21";
            gridColumn21.Visible = true;
            gridColumn21.VisibleIndex = 4;
            // 
            // CastleInfoGridView
            // 
            CastleInfoGridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { gridColumn1, gridColumn2, gridColumn3, gridColumn4, gridColumn5, gridColumn6, gridColumn9, gridColumn7, gridColumn8, gridColumn22 });
            CastleInfoGridView.GridControl = CastleInfoGridControl;
            CastleInfoGridView.Name = "CastleInfoGridView";
            CastleInfoGridView.OptionsDetail.AllowExpandEmptyDetails = true;
            CastleInfoGridView.OptionsView.EnableAppearanceEvenRow = true;
            CastleInfoGridView.OptionsView.EnableAppearanceOddRow = true;
            CastleInfoGridView.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Top;
            CastleInfoGridView.OptionsView.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;
            CastleInfoGridView.OptionsView.ShowGroupPanel = false;
            // 
            // gridColumn1
            // 
            gridColumn1.Caption = "名称";
            gridColumn1.FieldName = "Name";
            gridColumn1.Name = "gridColumn1";
            gridColumn1.Visible = true;
            gridColumn1.VisibleIndex = 0;
            // 
            // gridColumn2
            // 
            gridColumn2.ColumnEdit = MapLookUpEdit;
            gridColumn2.Caption = "地图";
            gridColumn2.FieldName = "Map";
            gridColumn2.Name = "gridColumn2";
            gridColumn2.Visible = true;
            gridColumn2.VisibleIndex = 1;
            // 
            // MapLookUpEdit
            // 
            MapLookUpEdit.AutoHeight = false;
            MapLookUpEdit.BestFitMode = DevExpress.XtraEditors.Controls.BestFitMode.BestFitResizePopup;
            MapLookUpEdit.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            MapLookUpEdit.Columns.AddRange(new DevExpress.XtraEditors.Controls.LookUpColumnInfo[] { new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Index", "索引"), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("FileName", "文件名"), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Description", "描述") });
            MapLookUpEdit.DisplayMember = "Description";
            MapLookUpEdit.Name = "MapLookUpEdit";
            MapLookUpEdit.NullText = "[地图为空]";
            // 
            // gridColumn3
            // 
            gridColumn3.Caption = "开始时间";
            gridColumn3.FieldName = "StartTime";
            gridColumn3.Name = "gridColumn3";
            gridColumn3.Visible = true;
            gridColumn3.VisibleIndex = 2;
            // 
            // gridColumn4
            // 
            gridColumn4.Caption = "持续时间";
            gridColumn4.FieldName = "Duration";
            gridColumn4.Name = "gridColumn4";
            gridColumn4.Visible = true;
            gridColumn4.VisibleIndex = 3;
            // 
            // gridColumn5
            // 
            gridColumn5.ColumnEdit = RegionLookUpEdit;
            gridColumn5.Caption = "目标区域";
            gridColumn5.FieldName = "ObjectiveRegion";
            gridColumn5.Name = "gridColumn5";
            gridColumn5.Visible = true;
            gridColumn5.VisibleIndex = 5;
            // 
            // RegionLookUpEdit
            // 
            RegionLookUpEdit.AutoHeight = false;
            RegionLookUpEdit.BestFitMode = DevExpress.XtraEditors.Controls.BestFitMode.BestFitResizePopup;
            RegionLookUpEdit.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            RegionLookUpEdit.Columns.AddRange(new DevExpress.XtraEditors.Controls.LookUpColumnInfo[] { new DevExpress.XtraEditors.Controls.LookUpColumnInfo("ServerDescription", "服务器描述") });
            RegionLookUpEdit.DisplayMember = "ServerDescription";
            RegionLookUpEdit.Name = "RegionLookUpEdit";
            RegionLookUpEdit.NullText = "[区域为空]";
            // 
            // gridColumn6
            // 
            gridColumn6.ColumnEdit = RegionLookUpEdit;
            gridColumn6.Caption = "进攻方出生区域";
            gridColumn6.FieldName = "AttackSpawnRegion";
            gridColumn6.Name = "gridColumn6";
            gridColumn6.Visible = true;
            gridColumn6.VisibleIndex = 6;
            // 
            // gridColumn9
            // 
            gridColumn9.ColumnEdit = ItemLookUpEdit;
            gridColumn9.Caption = "物品";
            gridColumn9.FieldName = "Item";
            gridColumn9.Name = "gridColumn9";
            gridColumn9.Visible = true;
            gridColumn9.VisibleIndex = 7;
            // 
            // ItemLookUpEdit
            // 
            ItemLookUpEdit.AutoHeight = false;
            ItemLookUpEdit.BestFitMode = DevExpress.XtraEditors.Controls.BestFitMode.BestFitResizePopup;
            ItemLookUpEdit.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            ItemLookUpEdit.Columns.AddRange(new DevExpress.XtraEditors.Controls.LookUpColumnInfo[] { new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Index", "索引"), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("ItemName", "物品名称"), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("ItemType", "物品类型"), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Price", "价格"), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("StackSize", "堆叠上限") });
            ItemLookUpEdit.DisplayMember = "ItemName";
            ItemLookUpEdit.Name = "ItemLookUpEdit";
            ItemLookUpEdit.NullText = "[物品为空]";
            // 
            // gridColumn7
            // 
            gridColumn7.ColumnEdit = MonsterLookUpEdit;
            gridColumn7.Caption = "怪物";
            gridColumn7.FieldName = "Monster";
            gridColumn7.Name = "gridColumn7";
            gridColumn7.Visible = true;
            gridColumn7.VisibleIndex = 8;
            // 
            // gridColumn8
            // 
            gridColumn8.Caption = "折扣";
            gridColumn8.FieldName = "Discount";
            gridColumn8.Name = "gridColumn8";
            gridColumn8.Visible = true;
            gridColumn8.VisibleIndex = 9;
            // 
            // ribbon
            // 
            ribbon.ExpandCollapseItem.Id = 0;
            ribbon.Items.AddRange(new DevExpress.XtraBars.BarItem[] { ribbon.ExpandCollapseItem, ribbon.SearchEditItem, SaveButton, ImportButton, ExportButton });
            ribbon.Location = new System.Drawing.Point(0, 0);
            ribbon.MaxItemId = 4;
            ribbon.Name = "ribbon";
            ribbon.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] { ribbonPage1 });
            ribbon.Size = new System.Drawing.Size(728, 144);
            // 
            // SaveButton
            // 
            SaveButton.Caption = "保存数据库";
            SaveButton.Id = 1;
            SaveButton.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("SaveButton.ImageOptions.Image");
            SaveButton.ImageOptions.LargeImage = (System.Drawing.Image)resources.GetObject("SaveButton.ImageOptions.LargeImage");
            SaveButton.LargeWidth = 60;
            SaveButton.Name = "SaveButton";
            SaveButton.ItemClick += SaveButton_ItemClick;
            // 
            // ImportButton
            // 
            ImportButton.Caption = "导入";
            ImportButton.Id = 2;
            ImportButton.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("ImportButton.ImageOptions.Image");
            ImportButton.ImageOptions.LargeImage = (System.Drawing.Image)resources.GetObject("ImportButton.ImageOptions.LargeImage");
            ImportButton.Name = "ImportButton";
            ImportButton.ItemClick += ImportButton_ItemClick;
            // 
            // ExportButton
            // 
            ExportButton.Caption = "导出";
            ExportButton.Id = 3;
            ExportButton.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("ExportButton.ImageOptions.Image");
            ExportButton.ImageOptions.LargeImage = (System.Drawing.Image)resources.GetObject("ExportButton.ImageOptions.LargeImage");
            ExportButton.Name = "ExportButton";
            ExportButton.ItemClick += ExportButton_ItemClick;
            // 
            // ribbonPage1
            // 
            ribbonPage1.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] { ribbonPageGroup1, JsonImportExport });
            ribbonPage1.Name = "ribbonPage1";
            ribbonPage1.Text = "主页";
            // 
            // ribbonPageGroup1
            // 
            ribbonPageGroup1.AllowTextClipping = false;
            ribbonPageGroup1.CaptionButtonVisible = DevExpress.Utils.DefaultBoolean.False;
            ribbonPageGroup1.ItemLinks.Add(SaveButton);
            ribbonPageGroup1.Name = "ribbonPageGroup1";
            ribbonPageGroup1.Text = "保存";
            // 
            // JsonImportExport
            // 
            JsonImportExport.ItemLinks.Add(ImportButton);
            JsonImportExport.ItemLinks.Add(ExportButton);
            JsonImportExport.Name = "JsonImportExport";
            JsonImportExport.Text = "JSON";
            // 
            // repositoryItemTextEdit1
            // 
            repositoryItemTextEdit1.AutoHeight = false;
            repositoryItemTextEdit1.Name = "repositoryItemTextEdit1";
            // 
            // gridColumn22
            // 
            gridColumn22.ColumnEdit = RegionLookUpEdit;
            gridColumn22.Caption = "城堡区域";
            gridColumn22.FieldName = "CastleRegion";
            gridColumn22.Name = "gridColumn22";
            gridColumn22.Visible = true;
            gridColumn22.VisibleIndex = 4;
            // 
            // CastleInfoView
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(728, 517);
            Controls.Add(CastleInfoGridControl);
            Controls.Add(ribbon);
            Name = "CastleInfoView";
            Ribbon = ribbon;
            Text = "城堡信息";
            ((System.ComponentModel.ISupportInitialize)FlagGridView).EndInit();
            ((System.ComponentModel.ISupportInitialize)MonsterLookUpEdit).EndInit();
            ((System.ComponentModel.ISupportInitialize)CastleInfoGridControl).EndInit();
            ((System.ComponentModel.ISupportInitialize)GateGridView).EndInit();
            ((System.ComponentModel.ISupportInitialize)GuardGridView).EndInit();
            ((System.ComponentModel.ISupportInitialize)CastleInfoGridView).EndInit();
            ((System.ComponentModel.ISupportInitialize)MapLookUpEdit).EndInit();
            ((System.ComponentModel.ISupportInitialize)RegionLookUpEdit).EndInit();
            ((System.ComponentModel.ISupportInitialize)ItemLookUpEdit).EndInit();
            ((System.ComponentModel.ISupportInitialize)ribbon).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemTextEdit1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DevExpress.XtraBars.Ribbon.RibbonControl ribbon;
        private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPage1;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup1;
        private DevExpress.XtraBars.BarButtonItem SaveButton;
        private DevExpress.XtraGrid.GridControl CastleInfoGridControl;
        private DevExpress.XtraGrid.Views.Grid.GridView CastleInfoGridView;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn1;
        private DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit MonsterLookUpEdit;
        private DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit RegionLookUpEdit;
        private DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit MapLookUpEdit;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn2;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn3;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn4;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn5;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn6;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn7;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn8;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit repositoryItemTextEdit1;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn9;
        private DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit ItemLookUpEdit;
        private DevExpress.XtraBars.BarButtonItem ImportButton;
        private DevExpress.XtraBars.BarButtonItem ExportButton;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup JsonImportExport;
        private DevExpress.XtraGrid.Views.Grid.GridView FlagGridView;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn10;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn11;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn12;
        private DevExpress.XtraGrid.Views.Grid.GridView GateGridView;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn13;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn14;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn15;
        private DevExpress.XtraGrid.Views.Grid.GridView GuardGridView;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn16;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn17;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn18;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn19;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn20;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn21;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn22;
    }
}
