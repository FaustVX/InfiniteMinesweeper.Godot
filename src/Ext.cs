using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using InfiniteMinesweeper;

public static class Ext
{
    extension(InputEvent ev)
    {
        public void ProcessDragging(Camera2D camera)
        {
            if (ev is InputEventMouseMotion motion && camera.IsMousePressed)
            {
                camera.IsMouseDragging = true;
                camera.GlobalPosition -= motion.Relative / camera.Zoom;
            }

            if (ev is InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Middle or MouseButton.Right } btn && !(camera.IsMousePressed = btn.Pressed))
                camera.IsMouseDragging = false;
        }
    }

    extension(Shortcut shortcut)
    {
        public bool IsPressed(InputEvent @event)
        {
            foreach (InputEvent item in shortcut.Events)
                if (item.IsMatch(@event) && @event.IsPressed())
                    return true;
            return false;
        }
        public bool IsReleased(InputEvent @event)
        {
            foreach (InputEvent item in shortcut.Events)
                if (item.IsMatch(@event) && @event.IsReleased())
                    return true;
            return false;
        }
    }

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

public static class GameSelectExt
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

        public void ResetPos()
        => _table.AddOrUpdate(map, new());
    }

    private static readonly ConditionalWeakTable<Map, Info> _table = [];

    private sealed class Info
    {
        public Pos? Pos1 { get; set; }
        public Pos? Pos2 { get; set; }
    }
}

public static class GameAutoSaveExt
{
    extension(Map map)
    {
        public int MaxAutosave
        => _table.GetOrCreateValue(map).MaxAutosave;
        public int LastSave
        => _table.GetOrCreateValue(map).LastSave;

        public int IncrementSave()
        {
            var info = _table.GetOrCreateValue(map);
            info.LastSave++;
            return info.LastSave %= info.MaxAutosave;
        }
    }

    private static readonly ConditionalWeakTable<Map, Info> _table = [];

    private sealed class Info
    {
        public int MaxAutosave { get; set; } = 15;
        public int LastSave { get; set; }
    }
}

public static class CellExt
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

public static class CameraDraggingExt
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
