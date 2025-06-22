using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using InfiniteMinesweeper;

public partial class Map : Node2D
{
    [Export]
    public required TileMapLayer MineField { get; set; }
    [Export]
    public required Camera2D Camera { get; set; }
    [Export]
    public required int Seed { get; set; } = Random.Shared.Next();
    [Export(PropertyHint.Range, "1, 50, or_greater")]
    public required int MinesPerChunk { get; set; } = 10;
    [Export]
    public required Label Label { get; set; }
    [Export]
    public required CheckBox ShowRemainingMines { get; set; }
    [Export]
    public required Button ZoomIn { get; set; }
    [Export]
    public required Button ZoomOut { get; set; }

    private Game _game;

    public override void _Ready()
    {
        _game = new(Seed, MinesPerChunk);
        if (OS.GetName() is "Android" or "iOS")
        {
            Camera.Zoom *= 4;

            Label.ResizeControl(4);
            ShowRemainingMines.ResizeControl(4);
            ZoomIn.ResizeControl(4);
            ZoomOut.ResizeControl(4);
        }

        ShowRemainingMines.Pressed += QueueRedraw;
        ZoomIn.Pressed += ZoomInHandler;
        ZoomOut.Pressed += ZoomOutHandler;
    }

    private void ZoomInHandler()
    {
        ZoomAtCursor(true);
        QueueRedraw();
    }

    private void ZoomOutHandler()
    {
        ZoomAtCursor(false);
        QueueRedraw();
    }

    public override void _Input(InputEvent @event)
    {
        ProcessAction(@event);
        @event.ProcessDragging(Camera);

        var chunk = _game.GetChunk(MineField.GlobalToMap(GetGlobalMousePosition()).AsPos.ToChunkPos(out _), ChunkState.NotGenerated);
        Label.Text = $"Remaining Mines: {chunk.RemainingMines}";

        void ProcessAction(InputEvent @event)
        {
            if (Camera.IsMouseDragging || !@event.IsActionType() || (@event is InputEventMouse && GetTree().CurrentScene.GetAllChildren<Control>().Any(static c => c.HasMouseOver)))
                return;
            var localPosition = MineField.GlobalToMap(GetGlobalMousePosition());
            if (@event.IsExplore)
                try
                {
                    _game.Explore(localPosition.AsPos);
                }
                catch (ExplodeException)
                { }
            else if (@event.IsFlag)
                _game.ToggleFlag(localPosition.AsPos);
            else if (@event.IsZoomIn)
                ZoomAtCursor(zoomIn: true);
            else if (@event.IsZoomOut)
                ZoomAtCursor(zoomIn: false);
            else if (@event.IsShowRemainingMines)
                ShowRemainingMines.ButtonPressed ^= true; // toggle bool
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        // Get the visible rectangle in world coordinates
        var visibleRect = Camera.GetViewportRect();
        var halfSize = (visibleRect.Size * 0.5f) * Camera.Zoom;
        // Convert world coordinates to map cell coordinates
        var cellTopLeft = MineField.GlobalToMap(Camera.GetScreenCenterPosition() - halfSize);
        var cellBottomRight = MineField.GlobalToMap(Camera.GetScreenCenterPosition() + halfSize);

        // Iterate over all cells in the visible area
        for (var x = cellTopLeft.X; x <= cellBottomRight.X; x++)
            for (var y = cellTopLeft.Y; y <= cellBottomRight.Y; y++)
            {
                ref var cell = ref _game.GetCell(new(x, y), ChunkState.NotGenerated);
                MineField.SetCell(new(x, y), new Pos(x, y).ToChunkPos(out _).IsEven ? 1 : 0, ShowRemainingMines.ButtonPressed ? cell.AtlasWithRemainingMines(_game) : cell.Atlas);
            }
    }

    private void ZoomAtCursor(bool zoomIn)
    {
        // https://forum.godotengine.org/t/how-to-zoom-camera-to-mouse/37348/2
        var mouseWorldPos = GetGlobalMousePosition();
        Camera.Zoom *= zoomIn ? 1.25f : 0.8f;
        var newMouseWorldPos = GetGlobalMousePosition();
        Camera.Position += mouseWorldPos - newMouseWorldPos;
    }
}

file static class Ext
{
    extension(TileMapLayer tileMap)
    {
        public Vector2I GlobalToMap(Vector2 globalPosition)
        => tileMap.LocalToMap(tileMap.ToLocal(globalPosition));

        /// <inheritdoc cref="TileMapLayer.SetCell(Vector2I, int, Vector2I?, int)"/>
        public void SetCell(Vector2I localPosition, Vector2I atlasCoords)
        => tileMap.SetCell(localPosition, 0, atlasCoords);
    }

    extension(Vector2I vector)
    {
        public Pos AsPos => new(vector.X, vector.Y);
    }

    extension(Pos pos)
    {
        public bool IsEven => (pos.X + pos.Y) % 2 == 0;
    }

    extension(InputEvent ev)
    {
        public bool IsExplore => ev.IsActionReleased("Explore", true);
        public bool IsFlag => ev.IsActionReleased("Flag", true);
        public bool IsZoomIn => ev.IsActionPressed("ZoomIn", true) || ev is InputEventMagnifyGesture { Factor: > 0 };
        public bool IsZoomOut => ev.IsActionPressed("ZoomOut", true) || ev is InputEventMagnifyGesture { Factor: < 0 };
        public bool IsShowRemainingMines => ev.IsActionPressed("ShowRemainingMines", true);
        public void ProcessDragging(Camera2D camera)
        {
            if (ev is InputEventMouseMotion motion && camera.IsMousePressed)
            {
                camera.IsMouseDragging = true;
                camera.GlobalPosition -= motion.Relative / camera.Zoom;
            }

            if (ev is InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Right } btn && !(camera.IsMousePressed = btn.Pressed))
                camera.IsMouseDragging = false;
        }
    }

    extension(Control control)
    {
        public bool HasMouseOver => control.GetGlobalRect().HasPoint(control.GetGlobalMousePosition());
        public void ResizeControl(float scale)
        {
            var anchorLeft = control.AnchorLeft;
            var anchorTop = control.AnchorTop;
            var anchorRight = control.AnchorRight;
            var anchorBottom = control.AnchorBottom;
            control.Scale *= scale;
            control.AnchorLeft = anchorLeft;
            control.AnchorTop = anchorTop;
            control.AnchorRight = anchorRight;
            control.AnchorBottom = anchorBottom;
        }
    }

    extension(Node node)
    {
        public IEnumerable<T> GetAllChildren<T>()
        where T : Node
        {
            if (node is T item)
                yield return item;
            foreach (var child in node.GetChildren())
                foreach (var n in child.GetAllChildren<T>())
                    yield return n;
        }
    }
}

file static class CameraExt
{
    extension(Camera2D camera)
    {
        public bool IsMouseDragging
        {
            get => _table.GetOrCreateValue(camera).IsMouseDragging;
            set => _table.GetOrCreateValue(camera).IsMouseDragging = value;
        }
        public bool IsMousePressed
        {
            get => _table.GetOrCreateValue(camera).IsMousePressed;
            set => _table.GetOrCreateValue(camera).IsMousePressed = value;
        }
    }

    private static readonly ConditionalWeakTable<Camera2D, Info> _table = [];

    private sealed class Info
    {
        public bool IsMouseDragging { get; set; }
        public bool IsMousePressed{ get; set; }
    }
}

file static class CellExt
{
    extension(ref readonly Cell cell)
    {
        private (int minesAround, bool isMine, bool isFlagged, bool isUnexplored) ToCacheKey => (cell.MinesAround, cell.IsMine, cell.IsFlagged, cell.IsUnexplored);
        private (int minesAround, bool isMine, bool isFlagged, bool isUnexplored) ToCacheKeyWithRemainingMines(Game game) => (cell.RemainingMines(game), cell.IsMine, cell.IsFlagged, cell.IsUnexplored);
        public Vector2I Atlas => AtlasCoordCache[cell.ToCacheKey];
        public Vector2I AtlasWithRemainingMines(Game game) => AtlasCoordCache[cell.ToCacheKeyWithRemainingMines(game)];
    }

    private static readonly FrozenDictionary<(int minesAround, bool isMine, bool isFlagged, bool isUnexplored), Vector2I> AtlasCoordCache
        = FrozenDictionary.ToFrozenDictionary(GenerateCache());

    private static IEnumerable<KeyValuePair<(int, bool, bool, bool), Vector2I>> GenerateCache()
    {
        int[] nums = [.. Enumerable.Range(-8, 17)];
        bool[] bools = [true, false];
        foreach (var mines in nums)
            foreach (var isMine in bools)
                foreach (var isFlagged in bools)
                    foreach (var isUnexplored in bools)
                        yield return new((mines, isMine, isFlagged, isUnexplored), (mines, isMine, isFlagged, isUnexplored) switch
                        {
                            ( _   , _    , false, true  ) => new(1        , 2), // default
                            ( _   , _    , true , true  ) => new(2        , 2), // flag
                            ( _   , true , _    , false ) => new(2        , 3), // mine
                            ( 0   , _    , _    , false ) => new(0        , 2), // empty
                            ( < 0 , _    , _    , false ) => new(0        , 3), // negative mines arround (too many flags)
                            ( <= 4, _    , _    , false ) => new(mines - 1, 0), // number
                            ( <= 8, _    , _    , false ) => new(mines - 5, 1), // number
                            ( _   , _    , _    , _     ) => new(3        , 3), // error
                        });
    }
}
