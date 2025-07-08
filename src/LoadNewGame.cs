using Godot;

public partial class LoadNewGame : LoadScene
{
	[Export]
	public SpinBox Seed { get; set; }
	[Export]
	public HSlider MinesPerChunk { get; set; }

	public void OnPressed()
	{
		var map = ResourceLoader.Load<PackedScene>(Scene).Instantiate<Map>();
		map.Seed = (int)Seed.Value;
		map.MinesPerChunk = (int)MinesPerChunk.Value;
		ChangeSceneToNode(this, map);
	}
}
