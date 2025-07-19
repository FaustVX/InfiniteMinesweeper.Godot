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

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var x = Math.Clamp(Zoom.X, MinZoom, MaxZoom);
		var y = Math.Clamp(Zoom.Y, MinZoom, MaxZoom);
		Zoom = new(x, y);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsZoomIn)
			ZoomInAtCursor();
		else if (@event.IsZoomOut)
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
	=> ZoomAtCursor(true);

	public void ZoomOutAtCursor()
	=> ZoomAtCursor(false);
}
