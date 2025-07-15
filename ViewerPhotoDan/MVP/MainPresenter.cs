using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ViewerPhotoDan.Services;

namespace ViewerPhotoDan.MVP
{
    public class MainPresenter
    {
        private readonly IMainView _view;
        private readonly IImageService _imageService;
        private List<string> _imageFilePaths = new List<string>();
        private ImageList _thumbnailImageList = new ImageList();

        private bool _isDragging = false;
        private Point _lastMousePosition;

        private Size _normalThumbnailSize = new Size(120, 120);
        private Size _doubleThumbnailSize = new Size(240, 240);

        public MainPresenter(IMainView view, IImageService imageService)
        {
            _view = view;
            _imageService = imageService;

            _view.LoadForm += OnLoadForm;
            _view.BeforeExpandNode += OnBeforeExpandNode;
            _view.AfterSelectNode += OnAfterSelectNode;
            _view.SelectedImageChanged += OnSelectedImageChanged;
            _view.ZoomInClicked += OnZoomInClicked;
            _view.ZoomOutClicked += OnZoomOutClicked;
            _view.FitToScreenClicked += OnFitToScreenClicked;
            _view.RetrieveVirtualItem += OnRetrieveVirtualItem;
            _view.CacheVirtualItems += OnCacheVirtualItems;
            _view.PictureBoxMouseDown += OnPictureBoxMouseDown;
            _view.PictureBoxMouseMove += OnPictureBoxMouseMove;
            _view.PictureBoxMouseUp += OnPictureBoxMouseUp;
            _view.ThumbnailSizeNormalClicked += OnThumbnailSizeNormalClicked;
            _view.ThumbnailSizeDoubleClicked += OnThumbnailSizeDoubleClicked;

            _thumbnailImageList.ImageSize = _normalThumbnailSize;
            _view.SetListViewImageList(_thumbnailImageList);
        }

        private void OnLoadForm(object? sender, EventArgs e)
        {
            PopulateTreeView();
        }

        private void PopulateTreeView()
        {
            _view.ClearTreeNodes();
            string[] drives = Environment.GetLogicalDrives();
            foreach (string drive in drives)
            {
                DriveInfo di = new DriveInfo(drive);
                if (di.IsReady)
                {
                    TreeNode rootNode = new TreeNode(drive)
                    {                   
                        Tag = drive,
                        ImageIndex = 0
                    };
                    _view.AddTreeNode(rootNode);
                    rootNode.Nodes.Add(new TreeNode()); // Add a dummy node
                }
            }
        }

        private void OnBeforeExpandNode(object? sender, TreeViewCancelEventArgs e)
        {
            if (e.Node != null && e.Node.Nodes.Count > 0)
            {
                if (e.Node.Nodes[0].Text == "" && e.Node.Nodes[0].Tag == null)
                {
                    e.Node.Nodes.Clear();
                    string? path = e.Node.Tag as string;
                    if (path != null)
                    {
                        try
                        {
                            string[] dirs = Directory.GetDirectories(path);
                            foreach (string dir in dirs)
                            {
                                DirectoryInfo di = new DirectoryInfo(dir);
                                TreeNode node = new TreeNode(di.Name, 0, 1)
                                {
                                    Tag = dir,
                                    ImageIndex = 0
                                };
                                try
                                {
                                    if (di.GetDirectories().Length > 0)
                                        node.Nodes.Add(new TreeNode());
                                }
                                catch (UnauthorizedAccessException) { /* Ignore inaccessible directories */ }
                                e.Node.Nodes.Add(node);
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            _view.ShowErrorMessage("Access Denied", $"Cannot access directory: {path}\n{ex.Message}");
                        }
                        catch (DirectoryNotFoundException ex)
                        {
                            _view.ShowErrorMessage("Directory Not Found", $"Directory not found: {path}\n{ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            _view.ShowErrorMessage("Error", $"An unexpected error occurred: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void OnAfterSelectNode(object? sender, TreeViewEventArgs e)
        {
            if (e.Node != null && e.Node.Tag != null)
            {
                string? path = e.Node.Tag as string;
                if (path != null && Directory.Exists(path))
                {
                    LoadImages(path);
                }
                else if (path != null)
                {
                    _view.ShowErrorMessage("Directory Not Found", $"The selected directory does not exist or is inaccessible: {path}");
                }
            }
        }

        private void LoadImages(string dir)
        {
            _view.ClearListView();
            _imageFilePaths.Clear();
            _thumbnailImageList.Images.Clear();
            _view.DisposePreviewImage(); // Dispose of the currently displayed image

            try
            {
                _view.SetListViewLoadingIndicator(true);
                _imageFilePaths.AddRange(_imageService.GetImageFiles(dir));
                _view.SetListViewVirtualListSize(_imageFilePaths.Count);
            }
            catch (UnauthorizedAccessException ex)
            {
                _view.ShowErrorMessage("Access Denied", $"Cannot access directory: {dir}\n{ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                _view.ShowErrorMessage("Directory Not Found", $"Directory not found: {dir}\n{ex.Message}");
            }
            catch (Exception ex)
            {
                _view.ShowErrorMessage("Error", $"An unexpected error occurred while loading images: {ex.Message}");
            }
            finally
            {
                _view.SetListViewLoadingIndicator(false);
            }
        }

        private void OnRetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex >= 0 && e.ItemIndex < _imageFilePaths.Count)
            {
                string imagePath = _imageFilePaths[e.ItemIndex];
                Image? thumbnail = null;
                try
                {
                    // Try to get thumbnail from cache
                    if (!_imageService.TryGetCachedThumbnail(imagePath, _thumbnailImageList.ImageSize, out thumbnail))
                    {
                        // If not in cache, create and save to cache
                        thumbnail = _imageService.CreateThumbnail(imagePath, _thumbnailImageList.ImageSize);
                        _imageService.SaveThumbnailToCache(imagePath, _thumbnailImageList.ImageSize, thumbnail);
                    }

                    // Add to ImageList if not already present and not null
                    if (thumbnail != null && !_thumbnailImageList.Images.ContainsKey(imagePath))
                    {
                        _thumbnailImageList.Images.Add(imagePath, thumbnail);
                    }
                    e.Item = new ListViewItem(Path.GetFileName(imagePath), _thumbnailImageList.Images.IndexOfKey(imagePath));
                    e.Item.Tag = imagePath;
                }
                catch (Exception) // Removed 'ex' variable
                {
                    // Provide a placeholder item if image loading fails
                    e.Item = new ListViewItem(Path.GetFileName(imagePath) + " (Error)");
                    e.Item.Tag = imagePath;
                    // Optionally, log the error or show a message for debugging
                    // _view.ShowErrorMessage("Thumbnail Error", $"Could not load thumbnail for {Path.GetFileName(imagePath)}: {ex.Message}");
                }
            }
            else
            {
                // Provide a default empty item if index is out of bounds
                e.Item = new ListViewItem("Invalid Item");
            }
        }

        private void OnCacheVirtualItems(object? sender, CacheVirtualItemsEventArgs e)
        {
            // This event is used to pre-fetch items that are likely to be displayed soon.
            // For simplicity, we are not implementing complex caching here, as ImageList handles some caching.
            // In a more complex scenario, you might load a batch of thumbnails here.
        }

        private async void OnSelectedImageChanged(object? sender, EventArgs e)
        {
            int selectedIndex = _view.SelectedImageIndex;
            if (selectedIndex >= 0 && selectedIndex < _imageFilePaths.Count)
            {
                string imagePath = _imageFilePaths[selectedIndex];
                try
                {
                    _view.SetPictureBoxLoadingIndicator(true);
                    _view.DisposePreviewImage(); // Dispose of the previous image
                    Image loadedImage = await Task.Run(() => _imageService.LoadImageFromFile(imagePath));
                    _view.SetPreviewImage(loadedImage);
                    // Reset zoom and pan when a new image is loaded
                    OnFitToScreenClicked(null, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    _view.ShowErrorMessage("Error", "Error loading image: " + ex.Message);
                }
                finally
                {
                    _view.SetPictureBoxLoadingIndicator(false);
                }
            }
        }

        private void OnZoomInClicked(object? sender, EventArgs e)
        {
            Image? currentImage = _view.GetPreviewImage();
            if (currentImage == null) return;

            _view.SetPictureBoxSizeMode(PictureBoxSizeMode.Normal);
            _view.SetPictureBoxDock(DockStyle.None);

            int newWidth = (int)(currentImage.Width * 1.2);
            int newHeight = (int)(currentImage.Height * 1.2);

            _view.SetPictureBoxSize(new Size(newWidth, newHeight));
        }

        private void OnZoomOutClicked(object? sender, EventArgs e)
        {
            Image? currentImage = _view.GetPreviewImage();
            if (currentImage == null) return;

            _view.SetPictureBoxSizeMode(PictureBoxSizeMode.Normal);
            _view.SetPictureBoxDock(DockStyle.None);

            int newWidth = (int)(currentImage.Width / 1.2);
            int newHeight = (int)(currentImage.Height / 1.2);

            _view.SetPictureBoxSize(new Size(newWidth, newHeight));

            // If zoomed out enough to fit, reset to FitToScreen
            if (newWidth < _view.PictureBoxContainerClientSize.Width && newHeight < _view.PictureBoxContainerClientSize.Height)
            {
                OnFitToScreenClicked(null, EventArgs.Empty);
            }
        }

        private void OnFitToScreenClicked(object? sender, EventArgs e)
        {
            _view.SetPictureBoxDock(DockStyle.Fill);
            _view.SetPictureBoxSizeMode(PictureBoxSizeMode.Zoom);
            _view.SetPictureBoxLocation(Point.Empty); // Reset pan position
        }

        private void OnPictureBoxMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _lastMousePosition = e.Location;
            }
        }

        private void OnPictureBoxMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                int dx = e.Location.X - _lastMousePosition.X;
                int dy = e.Location.Y - _lastMousePosition.Y;

                Point currentPictureBoxLocation = _view.GetPictureBoxLocation();
                Point newLocation = new Point(
                    currentPictureBoxLocation.X + dx,
                    currentPictureBoxLocation.Y + dy);

                // Apply pan limits
                Size pictureBoxSize = _view.GetPreviewImage()?.Size ?? Size.Empty;
                Size containerSize = _view.PictureBoxContainerClientSize;

                if (pictureBoxSize.Width > containerSize.Width)
                {
                    if (newLocation.X > 0) newLocation.X = 0;
                    if (newLocation.X < containerSize.Width - pictureBoxSize.Width) newLocation.X = containerSize.Width - pictureBoxSize.Width;
                }
                else
                {
                    newLocation.X = (containerSize.Width - pictureBoxSize.Width) / 2;
                }

                if (pictureBoxSize.Height > containerSize.Height)
                {
                    if (newLocation.Y > 0) newLocation.Y = 0;
                    if (newLocation.Y < containerSize.Height - pictureBoxSize.Height) newLocation.Y = containerSize.Height - pictureBoxSize.Height;
                }
                else
                {
                    newLocation.Y = (containerSize.Height - pictureBoxSize.Height) / 2;
                }

                _view.SetPictureBoxLocation(newLocation);
            }
        }

        private void OnPictureBoxMouseUp(object? sender, MouseEventArgs e)
        {
            _isDragging = false;
        }

        private void OnThumbnailSizeNormalClicked(object? sender, EventArgs e)
        {
            _view.SetThumbnailSize(_normalThumbnailSize);
            _thumbnailImageList.Images.Clear(); // Clear cache to force re-render
            _view.SetListViewVirtualListSize(_imageFilePaths.Count); // Trigger refresh
        }

        private void OnThumbnailSizeDoubleClicked(object? sender, EventArgs e)
        {
            _view.SetThumbnailSize(_doubleThumbnailSize);
            _thumbnailImageList.Images.Clear(); // Clear cache to force re-render
            _view.SetListViewVirtualListSize(_imageFilePaths.Count); // Trigger refresh
        }
    }
}