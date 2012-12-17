﻿using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.Utils;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using Xpand.ExpressApp.Win.ListEditors.GridListEditors.ColumnView;
using Xpand.ExpressApp.Win.ListEditors.GridListEditors.ColumnView.Design;
using Xpand.ExpressApp.Win.ListEditors.GridListEditors.GridView;
using ListView = DevExpress.ExpressApp.ListView;
using Xpand.Utils.Helpers;

namespace Xpand.ExpressApp.Win.SystemModule.ToolTip {
    [ModelAbstractClass]
    public interface IModelColumnTooltipData : IModelColumn {
        IModelTooltipData TooltipData { get; }
    }
    public interface IModelTooltipData : IModelToolTipController {
        [Category("DataOnToolTip")]
        bool DataOnToolTip { get; set; }
        [Category("DataOnToolTip")]
        int MaxHeight { get; set; }
        [Category("DataOnToolTip")]
        int MaxWidth { get; set; }
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        string ToolTipText { get; set; }

    }

    public class GridViewImageTextToolTipController : ViewController<ListView>, IModelExtender {
        ToolTipController _toolTipController;
        GridHitInfo _hotTrackInfo;

        protected override void OnViewControlsCreated() {
            base.OnViewControlsCreated();
            if (GridView == null) return;
            _toolTipController = new ToolTipController { ToolTipType = ToolTipType.SuperTip };
            _toolTipController.ReshowDelay = _toolTipController.InitialDelay;
            _toolTipController.AutoPopDelay = 10000;
            _toolTipController.AllowHtmlText = true;

            GridView.GridControl.MouseMove += GridControl_MouseMove;
            GridView.TopRowChanged += GridViewTopRowChanged;
            GridView.ShownEditor += delegate { HideHint(); };
            GridView.GridControl.MouseDown += delegate { HideHint(); };
            GridView.GridControl.MouseLeave += delegate { HideHint(); };
        }

        void HideHint() {
            _toolTipController.HideHint();
        }

        protected virtual DevExpress.XtraGrid.Views.Grid.GridView GridView {
            get {
                if (View != null) {
                    var gridListEditor = View.Editor as ColumnsListEditor;
                    if (gridListEditor != null) return gridListEditor.GridView();
                    var columnViewEditor = View.Editor as IColumnViewEditor;
                    if (columnViewEditor != null) return columnViewEditor.ColumnView as DevExpress.XtraGrid.Views.Grid.GridView;
                }
                return null;
            }
        }

        public GridHitInfo HotTrackInfo {
            get { return _hotTrackInfo; }
            set {
                if (!value.InRowCell) {
                    HideHint();
                    _hotTrackInfo = null;
                    return;
                }
                if (_hotTrackInfo != null && _hotTrackInfo.Column == value.Column && _hotTrackInfo.RowHandle == value.RowHandle)
                    return;
                _hotTrackInfo = value;

                var modelColumn = GetModelColumn();
                var columnTooltipData = ((IModelColumnTooltipData)modelColumn);
                if (TooltipEnabled(columnTooltipData)) {
                    if (!HotTrackInfo.InRowCell || IsEditing) {
                        HideHint();
                    } else
                        ShowToolTip(columnTooltipData);
                }
            }
        }

        bool TooltipEnabled(IModelColumnTooltipData columnTooltipData) {
            return columnTooltipData.TooltipData.DataOnToolTip || !string.IsNullOrEmpty(columnTooltipData.TooltipData.ToolTipText) || columnTooltipData.TooltipData.ToolTipController != null;
        }

        IModelColumn GetModelColumn() {
            var xafGridColumn = _hotTrackInfo.Column as XafGridColumn;
            if (xafGridColumn != null) return xafGridColumn.Model;
            var gridColumn = _hotTrackInfo.Column as IXafGridColumn;
            if (gridColumn != null) return gridColumn.Model;
            throw new NotImplementedException(_hotTrackInfo.Column.GetType().FullName);
        }

        bool IsEditing {
            get {
                return GridView.ActiveEditor != null &&
                       HotTrackInfo.Column == GridView.FocusedColumn &&
                       HotTrackInfo.RowHandle == GridView.FocusedRowHandle;
            }
        }
        protected override void Dispose(bool disposing) {
            if (_toolTipController != null)
                _toolTipController.Dispose();
            base.Dispose(disposing);
        }
        void ShowToolTip(IModelColumnTooltipData modelColumnTooltipData) {
            var toolTipControlInfo = new ToolTipControlInfo();
            var item = new ToolTipItem { ImageToTextDistance = 0 };
            var modelTooltipData = modelColumnTooltipData.TooltipData;
            if (modelTooltipData.DataOnToolTip) {
                var modelMember = modelColumnTooltipData.ModelMember;
                if (modelMember.MemberInfo.MemberType == typeof(Image)) {
                    var image = modelMember.MemberInfo.GetValue(GridView.GetRow(HotTrackInfo.RowHandle)) as Image;
                    if (modelTooltipData.MaxWidth > 0 && modelTooltipData.MaxHeight > 0)
                        image = image.CreateImage(modelTooltipData.MaxWidth, modelTooltipData.MaxHeight);
                    item.Image = image;
                } else
                    item.Text = string.Format("{0}", GridView.GetRowCellValue(HotTrackInfo.RowHandle, HotTrackInfo.Column));
            } else if (!string.IsNullOrEmpty(modelTooltipData.ToolTipText)) {
                item.Text = modelTooltipData.ToolTipText;
            } else {
                var controller = ObjectToolTipController(modelColumnTooltipData);
                controller.ShowHint(GridView.GetRow(HotTrackInfo.RowHandle), HotTrackInfo.HitPoint, ObjectSpace, _toolTipController);
                return;
            }
            toolTipControlInfo.Object = HotTrackInfo;
            toolTipControlInfo.SuperTip = new SuperToolTip();
            toolTipControlInfo.SuperTip.Items.Add(item);
            toolTipControlInfo.ToolTipPosition = Cursor.Position;
            _toolTipController.ShowHint(toolTipControlInfo);
        }

        ObjectToolTipController ObjectToolTipController(IModelColumnTooltipData modelColumnTooltipData) {
            var objects = new[] { View.Editor.Control };
            return (ObjectToolTipController)Activator.CreateInstance(modelColumnTooltipData.TooltipData.ToolTipController, objects);
        }

        void GridControl_MouseMove(object sender, MouseEventArgs e) {
            if (GridView != null)
                HotTrackInfo = GridView.CalcHitInfo(e.X, e.Y);
        }

        void GridViewTopRowChanged(object sender, EventArgs e) {
            _toolTipController.HideHint();
            HotTrackInfo = GridView.CalcHitInfo(GridView.GridControl.PointToClient(Cursor.Position));
        }
        #region Implementation of IModelExtender
        public void ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
            extenders.Add<IModelColumn, IModelColumnTooltipData>();
        }
        #endregion
    }
}