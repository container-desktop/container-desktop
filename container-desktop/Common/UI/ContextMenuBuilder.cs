using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ContainerDesktop.Common.UI
{
    public class ContextMenuBuilder
    {
        private readonly ContextMenuStrip _contextMenu;

        public ContextMenuBuilder()
        {
            _contextMenu = new ContextMenuStrip();
        }

        public ContextMenuBuilder AddMenuItem(string text, Action handler, string imageFileName = null, Keys shortcutKeys = Keys.None, ContextMenuBuilder subItemsBuilder = null)
        {
            var image = !string.IsNullOrWhiteSpace(imageFileName) && File.Exists(imageFileName) ? Image.FromFile(imageFileName) : null;
            var item = new ToolStripMenuItem(text);
            item.Click += (_, __) => handler();
            item.ShortcutKeys = shortcutKeys;
            item.Image = image;
            if (subItemsBuilder != null)
            {
                var strip = subItemsBuilder.Build();
                item.DropDownItems.AddRange(strip.Items);
            }
            _contextMenu.Items.Add(item);
            return this;
        }

        public ContextMenuBuilder AddSeperator()
        {
            var item = new ToolStripSeparator();
            _contextMenu.Items.Add(item);
            return this;
        }

        public ContextMenuStrip Build()
        {
            return _contextMenu;
        }
    }
}
