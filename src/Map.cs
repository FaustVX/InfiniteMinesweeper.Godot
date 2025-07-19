using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    [Export(PropertyHint.Range, "7, 35, or_greater")]
    public required int MinesPerChunk { get; set; } = 10;
    public Game Game { get; set; }

    [Signal]
    public delegate void ResizeEventHandler(float scale);
    [Signal]
    public delegate void InfoEventHandler(string message);

    private bool _showRemainingMines = true;
    private bool _showGroups;
    private HashSet<Pos>[] _groups;

    public override void _Ready()
    {
        if (Game is null)
            NewGame();
        if (OS.GetName() is "Android" or "iOS")
        {
            Camera.Zoom *= 4;
            EmitSignalResize(4);
        }
    }

    private void NewGame()
    {
        Game = new(Seed, MinesPerChunk);
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

    public void SaveHandler()
    {
        using var stream = File.Create("saves/game1.json");
        Game.Save(stream);
    }

    public void ResetGroups()
    => this.ResetPos();

    public void ToggleShowRemainingMines(bool showed)
    {
        _showRemainingMines = showed;
        QueueRedraw();
    }

    private void AutoSave()
    {
        if (!Directory.Exists("saves"))
            Directory.CreateDirectory("saves");
        var index = this.IncrementSave();
        using var stream = File.Create($"saves/autosave-{index}.json");
        Game.Save(stream);
    }

    public override void _Input(InputEvent @event)
    {
        ProcessAction(@event);
        @event.ProcessDragging(Camera);

        var chunk = Game.GetChunk(MineField.GlobalToMap(GetGlobalMousePosition()).AsPos.ToChunkPos(out _), ChunkState.NotGenerated);
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
            var localCellPosition = MineField.GlobalToMap(GetGlobalMousePosition()).AsPos;

            if (@event.IsExplore)
                if (_showGroups)
                {
                    if (this.AddPos(localCellPosition))
                        _groups = Game.GetCollidingGroups(this.Pos1, this.Pos2);
                    else
                        _groups = null;
                }
                else
                    try
                    {
                        Game.Explore(localCellPosition);
                        AutoSave();
                    }
                    catch (ExplodeException)
                    { }
            else if (!_showGroups)
                if (@event.IsFlag)
                {
                    Game.ToggleFlag(localCellPosition);
                    AutoSave();
                }
                else if (@event.IsExploreChunk)
                {
                    Game.TryClearChunk(localCellPosition.ToChunkPos(out _));
                    AutoSave();
                }
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
                ref var cell = ref Game.GetCell(new(x, y), ChunkState.NotGenerated);
                var sourceId = new Pos(x, y).ToChunkPos(out _) switch
                {
                    var p when Game.GetChunk(p, ChunkState.NotGenerated).HasExploded => 2,
                    var p when p.IsEven => 1,
                    _ => 0,
                };
                MineField.SetCell(new(x, y), sourceId, _showRemainingMines ? cell.AtlasWithRemainingMines(Game) : cell.Atlas);
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
}
