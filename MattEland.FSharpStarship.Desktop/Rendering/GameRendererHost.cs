﻿using System.Windows;
using System.Windows.Media;

namespace MattEland.FSharpStarship.Desktop.Rendering
{
    public class GameRendererHost : FrameworkElement
    {
        private GameRenderer _renderer = new GameRenderer();

        protected override Visual GetVisualChild(int index) => _renderer;
        protected override int VisualChildrenCount => 1;
    }

    public class TileRendererHost : FrameworkElement
    {
        private TileRenderer _renderer = new TileRenderer();

        protected override Visual GetVisualChild(int index) => _renderer;
        protected override int VisualChildrenCount => 1;
    }
}