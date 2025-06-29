using Godot;

public partial class LoadGame : LoadScene
{
	[Export]
	public required SpinBox Seed { get; set; }
	[Export]
	public required HSlider MinesPerChunk { get; set; }

	public void OnPressed()
	{
		var map = ResourceLoader.Load<PackedScene>(Scene).Instantiate<Map>();
		map.Seed = (int)Seed.Value;
		map.MinesPerChunk = (int)MinesPerChunk.Value;
		ChangeSceneToNode(this, map);
	}
}
