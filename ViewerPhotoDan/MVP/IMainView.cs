using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ViewerPhotoDan.MVP
{
    public interface IMainView
    {
        // Events
        event EventHandler LoadForm;
        event TreeViewCancelEventHandler BeforeExpandNode;
        event TreeViewEventHandler AfterSelectNode;
        event EventHandler SelectedImageChanged;
        event EventHandler ZoomInClicked;
        event EventHandler ZoomOutClicked;
        event EventHandler FitToScreenClicked;
        event RetrieveVirtualItemEventHandler RetrieveVirtualItem;
        event CacheVirtualItemsEventHandler CacheVirtualItems;
        event MouseEventHandler PictureBoxMouseDown;
        event MouseEventHandler PictureBoxMouseMove;
        event MouseEventHandler PictureBoxMouseUp;
        event EventHandler ThumbnailSizeNormalClicked;
        event EventHandler ThumbnailSizeDoubleClicked;

        // Properties
        string? SelectedNodeTag { get; }
        int SelectedImageIndex { get; }
        Size PictureBoxContainerClientSize { get; }

        // Methods
        void ClearTreeNodes();
        void AddTreeNode(TreeNode node);
        void ClearListView();
        void UpdateListViewItem(int index, ListViewItem item);
        void SetListViewImageList(ImageList imageList);
        void AddListViewItem(ListViewItem item);
        void SetPreviewImage(Image image);
        Image? GetPreviewImage();
        void DisposePreviewImage();
        void SetPictureBoxSizeMode(PictureBoxSizeMode sizeMode);
        void SetPictureBoxDock(DockStyle dockStyle);
        void ShowErrorMessage(string title, string message);
        void SetListViewVirtualMode(bool virtualMode);
        void SetListViewVirtualListSize(int size);
        void SetListViewLoadingIndicator(bool isLoading);
        void SetPictureBoxLoadingIndicator(bool isLoading);
        void SetPictureBoxLocation(Point location);
        Point GetPictureBoxLocation();
        void SetPictureBoxSize(Size size);
        void SetThumbnailSize(Size size);
    }
}