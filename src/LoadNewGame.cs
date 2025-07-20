using System;
using Godot;

public partial class LoadNewGame : LoadScene
{
	[Export]
	public SpinBox Seed { get; set; }
	[Export]
	public HSlider MinesPerChunk { get; set; }
	private bool _startup = false;

	public override void _EnterTree()
	{
		ReadOnlySpan<string> args = System.Environment.GetCommandLineArgs();
		if (args.IndexOf("--") is > 0 and var dash && args[(dash + 1)..] is [var arg] && int.TryParse(arg, out var seed))
		{
			Seed.Value = seed;
			_startup = true;
		}
    }

	public override void _Process(double delta)
	{
		if (_startup)
			OnPressed();
    }

	public void OnPressed()
	{
		try
		{
			EmitSignalLoading(true);
			var map = ResourceLoader.Load<PackedScene>(Scene).Instantiate<Map>();
			map.Seed = (int)Seed.Value;
			map.MinesPerChunk = (int)MinesPerChunk.Value;
			ChangeSceneToNode(this, map);
		}
		finally
		{
			EmitSignalLoading(false);
		}
	}
}
