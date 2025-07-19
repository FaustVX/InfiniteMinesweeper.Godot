using Godot;
using System;

public partial class Zoom : Camera2D
{
	[Export]
	public float MinZoom { get; set; } = 0;
	[Export]
	public float MaxZoom { get; set; } = float.MaxValue;
	[Export]
	public float ZoomValue
	{
		get => field;
		set
		{
			field = Math.Clamp(value, MinZoom, MaxZoom);
			ZoomAtCursor(field);
			EmitSignalIsMaxZoomOut(field <= MinZoom);
			EmitSignalIsMaxZoomIn(field >= MaxZoom);
			EmitSignalZoomChanged(field);
		}
	} = 1;
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
	[Signal]
	public delegate void ZoomChangedEventHandler(float zoom);

	public override void _Input(InputEvent @event)
	{
		if (ShortcutZoomIn.IsPressed(@event))
			ZoomInAtCursor();
		else if (ShortcutZoomOut.IsPressed(@event))
			ZoomOutAtCursor();
	}

    public void ZoomInOutAtCursor(bool zoomIn)
	=> ZoomValue *= zoomIn ? ZoomInFactor : ZoomOutFactor;

    public void ZoomAtCursor(float zoom)
	{
		// https://forum.godotengine.org/t/how-to-zoom-camera-to-mouse/37348/2
		var mouseWorldPos = GetGlobalMousePosition();
		Zoom = Vector2.One * zoom;
		var newMouseWorldPos = GetGlobalMousePosition();
		Position += mouseWorldPos - newMouseWorldPos;
	}

    public void ZoomAtCenter(float zoom)
	=> Zoom = Vector2.One * zoom;

    public void ZoomInAtCursor()
	=> ZoomInOutAtCursor(true);

    public void ZoomOutAtCursor()
	=> ZoomInOutAtCursor(false);
}
