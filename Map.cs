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
    public required TileMapLayer GroupsField { get; set; }
    [Export]
    public required Camera2D Camera { get; set; }
    [Export]
    public required int Seed
    {
        get => field is 0 ? Random.Shared.Next() : field;
        set => field = value;
    }
    [Export(PropertyHint.Range, "1, 50, or_greater")]
    public required int MinesPerChunk { get; set; } = 10;

    [Signal]
    public delegate void ResizeEventHandler(float scale);
    [Signal]
    public delegate void InfoEventHandler(string message);

    private Game _game;
    private bool _showRemainingMines;
    private bool _showGroups;
    private HashSet<Pos>[] _groups;

    public override void _Ready()
    {
        NewGame();
        if (OS.GetName() is "Android" or "iOS")
        {
            Camera.Zoom *= 4;
            EmitSignalResize(4);
        }
    }

    private void NewGame()
    {
        _game = new(Seed, MinesPerChunk);
    }

    public void ZoomInHandler()
    {
        ZoomAtCursor(true);
        QueueRedraw();
    }

    public void ZoomOutHandler()
    {
        ZoomAtCursor(false);
        QueueRedraw();
    }

    public void RestartHandler()
    {
        NewGame();
        QueueRedraw();
    }

    public void GroupsHandler(bool toggledOn)
    {
        _showGroups = toggledOn;
        QueueRedraw();
    }

    public void ToggleShowRemainingMines(bool showed)
    {
        _showRemainingMines = showed;
        QueueRedraw();
    }

    public override void _Input(InputEvent @event)
    {
        ProcessAction(@event);
        @event.ProcessDragging(Camera);

        var chunk = _game.GetChunk(MineField.GlobalToMap(GetGlobalMousePosition()).AsPos.ToChunkPos(out _), ChunkState.NotGenerated);
        EmitSignalInfo($"Remaining Mines: {chunk.RemainingMines}");

        void ProcessAction(InputEvent @event)
        {
            if (Camera.IsMouseDragging)
            {
                QueueRedraw();
                return;
            }

            if (!@event.IsActionType() || (@event is InputEventMouse && GetTree().CurrentScene.GetAllChildren<Control>().Any(static c => c.HasMouseOver)))
                return;
            var localPosition = MineField.GlobalToMap(GetGlobalMousePosition()).AsPos;

            if (@event.IsExplore)
                if (_showGroups)
                {
                    if (this.AddPos(localPosition))
                        _groups = _game.GetCollidingGroups(this.Pos1, this.Pos2);
                    else
                        _groups = null;
                }
                else
                    try
                    {
                        _game.Explore(localPosition);
                    }
                    catch (ExplodeException)
                    { }
            else if (!_showGroups && @event.IsFlag)
                _game.ToggleFlag(localPosition);
            else if (@event.IsZoomIn)
                ZoomAtCursor(zoomIn: true);
            else if (@event.IsZoomOut)
                ZoomAtCursor(zoomIn: false);
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        // Get the visible rectangle in world coordinates
        var visibleRect = Camera.GetViewportRect();
        var halfSize = (visibleRect.Size * 0.5f) / Camera.Zoom;
        // Convert world coordinates to map cell coordinates
        var cellTopLeft = MineField.GlobalToMap(Camera.GetScreenCenterPosition() - halfSize);
        var cellBottomRight = MineField.GlobalToMap(Camera.GetScreenCenterPosition() + halfSize);

        // Iterate over all cells in the visible area
        for (var x = cellTopLeft.X; x <= cellBottomRight.X; x++)
            for (var y = cellTopLeft.Y; y <= cellBottomRight.Y; y++)
            {
                ref var cell = ref _game.GetCell(new(x, y), ChunkState.NotGenerated);
                MineField.SetCell(new(x, y), new Pos(x, y).ToChunkPos(out _).IsEven ? 1 : 0, _showRemainingMines ? cell.AtlasWithRemainingMines(_game) : cell.Atlas);
                switch (_groups)
                {
                    case [var intersect]:
                        if (intersect.Contains(new(x, y)))
                            GroupsField.SetCell(new(x, y), 0, new(1, 0));
                        break;
                    case [var g1, var g2]:
                        if (g1.Contains(new(x, y)))
                            GroupsField.SetCell(new(x, y), 0, new(0, 0));
                        else if (g2.Contains(new(x, y)))
                            GroupsField.SetCell(new(x, y), 0, new(2, 0));
                        break;
                    case [var g1, var g2, var intersect]:
                        if (g1.Contains(new(x, y)))
                            GroupsField.SetCell(new(x, y), 0, new(0, 0));
                        else if (intersect.Contains(new(x, y)))
                            GroupsField.SetCell(new(x, y), 0, new(1, 0));
                        else if (g2.Contains(new(x, y)))
                            GroupsField.SetCell(new(x, y), 0, new(2, 0));
                        break;
                }
            }
        _groups = null;
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
        public bool IsZoomIn => ev.IsActionPressed("ZoomIn", true);
        public bool IsZoomOut => ev.IsActionPressed("ZoomOut", true);
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

file static class GameSelectExt
{
    extension(Map map)
    {
        public Pos Pos1
        => _table.GetOrCreateValue(map).Pos1 ?? throw new NotSupportedException();
        public Pos Pos2
        => _table.GetOrCreateValue(map).Pos2 ?? throw new NotSupportedException();

        public bool AddPos(Pos pos)
        {
            var info = _table.GetOrCreateValue(map);
            if (info.Pos1 is not null && info.Pos2 is null)
            {
                info.Pos2 = pos;
                return true;
            }
            info.Pos1 = pos;
            info.Pos2 = null;
            return false;
        }
    }

    private static readonly ConditionalWeakTable<Map, Info> _table = [];

    private sealed class Info
    {
        public Pos? Pos1 { get; set; }
        public Pos? Pos2 { get; set; }
    }
}

file static class CameraDraggingExt
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
        public bool IsMousePressed { get; set; }
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
