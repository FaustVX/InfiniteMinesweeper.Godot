using Godot;
using System;

public partial class Zoom : Camera2D
{
	[Export]
	public float MinZoom { get; set; } = 0;
	[Export]
	public float MaxZoom { get; set; } = float.MaxValue;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var x = Math.Clamp(Zoom.X, MinZoom, MaxZoom);
		var y = Math.Clamp(Zoom.Y, MinZoom, MaxZoom);
		Zoom = new(x, y);
	}
}
