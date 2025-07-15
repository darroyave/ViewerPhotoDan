using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ViewerPhotoDan.MVP;

namespace ViewerPhotoDan
{
    public partial class FormMain : Form, IMainView
    {
        private ToolStripStatusLabel _statusLabel;
        private Label _pictureBoxLoadingLabel;

        public FormMain()
        {
            InitializeComponent();
            listView1.VirtualMode = true;
            listView1.RetrieveVirtualItem += ListView1_RetrieveVirtualItem;
            listView1.CacheVirtualItems += ListView1_CacheVirtualItems;

            // Pan functionality events
            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseMove += PictureBox1_MouseMove;
            pictureBox1.MouseUp += PictureBox1_MouseUp;

            // Initialize status label
            _statusLabel = new ToolStripStatusLabel("Ready");
            statusStrip1.Items.Add(_statusLabel);

            // Initialize PictureBox loading indicator
            _pictureBoxLoadingLabel = new Label()
            {
                Text = "Loading image...",
                AutoSize = true,
                Location = new Point(10, 10), // Adjust as needed
                BackColor = Color.FromArgb(150, Color.Black), // Semi-transparent black
                ForeColor = Color.White,
                Padding = new Padding(5),
                Visible = false,
                Anchor = AnchorStyles.None // Center the label
            };
            panelPictureBox.Controls.Add(_pictureBoxLoadingLabel);
            _pictureBoxLoadingLabel.BringToFront();
            _pictureBoxLoadingLabel.Location = new Point(
                (panelPictureBox.Width - _pictureBoxLoadingLabel.Width) / 2,
                (panelPictureBox.Height - _pictureBoxLoadingLabel.Height) / 2
            );

            // Initialize ImageList for ListView
            listView1.LargeImageList = new ImageList();
        }

        // Events
        public event EventHandler? LoadForm;
        public event TreeViewCancelEventHandler? BeforeExpandNode;
        public event TreeViewEventHandler? AfterSelectNode;
        public event EventHandler? SelectedImageChanged;
        public event EventHandler? ZoomInClicked;
        public event EventHandler? ZoomOutClicked;
        public event EventHandler? FitToScreenClicked;
        public event RetrieveVirtualItemEventHandler? RetrieveVirtualItem;
        public event CacheVirtualItemsEventHandler? CacheVirtualItems;
        public event MouseEventHandler? PictureBoxMouseDown;
        public event MouseEventHandler? PictureBoxMouseMove;
        public event MouseEventHandler? PictureBoxMouseUp;
        public event EventHandler? ThumbnailSizeNormalClicked;
        public event EventHandler? ThumbnailSizeDoubleClicked;

        // Properties
        public string? SelectedNodeTag => treeView1.SelectedNode?.Tag as string;
        public int SelectedImageIndex => listView1.SelectedIndices.Count > 0 ? listView1.SelectedIndices[0] : -1;
        public Size PictureBoxContainerClientSize => panelPictureBox.ClientSize;

        // Methods
        public void ClearTreeNodes() => treeView1.Nodes.Clear();
        public void AddTreeNode(TreeNode node) => treeView1.Nodes.Add(node);
        public void ClearListView() => listView1.Items.Clear();
        public void UpdateListViewItem(int index, ListViewItem item)
        {
            if (index >= 0 && index < listView1.VirtualListSize)
            {
                listView1.Invalidate(item.Bounds);
            }
        }
        public void SetListViewImageList(ImageList imageList) => listView1.LargeImageList = imageList;
        public void AddListViewItem(ListViewItem item) => listView1.Items.Add(item);
        public void SetPreviewImage(Image image)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
            pictureBox1.Image = image;
        }
        public Image? GetPreviewImage() => pictureBox1.Image;
        public void DisposePreviewImage()
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }
        }
        public void SetPictureBoxSizeMode(PictureBoxSizeMode sizeMode) => pictureBox1.SizeMode = sizeMode;
        public void SetPictureBoxDock(DockStyle dockStyle) => pictureBox1.Dock = dockStyle;
        public void ShowErrorMessage(string title, string message) => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        public void SetListViewVirtualMode(bool virtualMode) => listView1.VirtualMode = virtualMode;
        public void SetListViewVirtualListSize(int size) => listView1.VirtualListSize = size;
        public void SetListViewLoadingIndicator(bool isLoading)
        {
            _statusLabel.Text = isLoading ? "Loading..." : "Ready";
            statusStrip1.Refresh();
        }
        public void SetPictureBoxLoadingIndicator(bool isLoading)
        {
            _pictureBoxLoadingLabel.Visible = isLoading;
        }
        public void SetPictureBoxLocation(Point location) => pictureBox1.Location = location;
        public Point GetPictureBoxLocation() => pictureBox1.Location;
        public void SetPictureBoxSize(Size size) => pictureBox1.Size = size;
        public void SetThumbnailSize(Size size)
        {
            if (listView1.LargeImageList != null) // Added null check
            {
                listView1.LargeImageList.ImageSize = size;
                listView1.Refresh(); // Refresh to apply new thumbnail size
            }
        }

        private void FormMain_Load(object? sender, EventArgs e) => LoadForm?.Invoke(sender, e);
        private void treeView1_BeforeExpand(object? sender, TreeViewCancelEventArgs e) => BeforeExpandNode?.Invoke(sender, e);
        private void treeView1_AfterSelect(object? sender, TreeViewEventArgs e) => AfterSelectNode?.Invoke(sender, e);
        private void listView1_SelectedIndexChanged(object? sender, EventArgs e) => SelectedImageChanged?.Invoke(sender, e);
        private void btnZoomIn_Click(object? sender, EventArgs e) => ZoomInClicked?.Invoke(sender, e);
        private void btnZoomOut_Click(object? sender, EventArgs e) => ZoomOutClicked?.Invoke(sender, e);
        private void btnFitToScreen_Click(object? sender, EventArgs e) => FitToScreenClicked?.Invoke(sender, e);
        private void btnThumbnailNormal_Click(object? sender, EventArgs e) => ThumbnailSizeNormalClicked?.Invoke(sender, e);
        private void btnThumbnailDouble_Click(object? sender, EventArgs e) => ThumbnailSizeDoubleClicked?.Invoke(sender, e);

        private void ListView1_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
        {
            RetrieveVirtualItem?.Invoke(sender, e);
        }

        private void ListView1_CacheVirtualItems(object? sender, CacheVirtualItemsEventArgs e)
        {
            CacheVirtualItems?.Invoke(sender, e);
        }

        private void PictureBox1_MouseDown(object? sender, MouseEventArgs e)
        {
            PictureBoxMouseDown?.Invoke(sender, e);
        }

        private void PictureBox1_MouseMove(object? sender, MouseEventArgs e)
        {
            PictureBoxMouseMove?.Invoke(sender, e);
        }

        private void PictureBox1_MouseUp(object? sender, MouseEventArgs e)
        {
            PictureBoxMouseUp?.Invoke(sender, e);
        }
    }
}