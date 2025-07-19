using Godot;
using System;

public partial class Zoom : Camera2D
{
	[Export]
	public float MinZoom { get; set; } = 0;
	[Export]
	public float MaxZoom { get; set; } = float.MaxValue;
	[Export]
	public float ZoomInFactor { get; set; } = 1.25f;
	[Export]
	public float ZoomOutFactor { get; set; } = .75f;
    [Export, ExportGroup("Shortcuts", prefix: "Shortcut")]
	public Shortcut ShortcutZoomIn { get; set; } = new Shortcut()
	{
		Events = {
			new InputEventMouseButton()
			{
				ButtonIndex = MouseButton.WheelUp
			}
		}
	};
    [Export]
    public Shortcut ShortcutZoomOut { get; set; } = new Shortcut()
	{
		Events = {
			new InputEventMouseButton()
			{
				ButtonIndex = MouseButton.WheelDown
			}
		}
	};

	[Signal]
	public delegate void IsMaxZoomInEventHandler(bool isMax);
	[Signal]
	public delegate void IsMaxZoomOutEventHandler(bool isMin);

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var x = Math.Clamp(Zoom.X, MinZoom, MaxZoom);
		var y = Math.Clamp(Zoom.Y, MinZoom, MaxZoom);
		EmitSignalIsMaxZoomOut(x <= MinZoom);
		EmitSignalIsMaxZoomIn(x >= MaxZoom);
		Zoom = new(x, y);
	}

	public override void _Input(InputEvent @event)
	{
		if (ShortcutZoomIn.IsPressed(@event))
			ZoomInAtCursor();
		else if (ShortcutZoomOut.IsPressed(@event))
			ZoomOutAtCursor();
	}

	public void ZoomAtCursor(bool zoomIn)
	{
		// https://forum.godotengine.org/t/how-to-zoom-camera-to-mouse/37348/2
		var mouseWorldPos = GetGlobalMousePosition();
		Zoom *= zoomIn ? ZoomInFactor : ZoomOutFactor;
		var newMouseWorldPos = GetGlobalMousePosition();
		Position += mouseWorldPos - newMouseWorldPos;
	}

    public void ZoomInAtCursor()
    {
		if (Zoom.X < MaxZoom)
        	ZoomAtCursor(true);
    }

    public void ZoomOutAtCursor()
    {
		if (Zoom.X > MinZoom)
	        ZoomAtCursor(false);
    }
}
