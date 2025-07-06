using Godot;
using InfiniteMinesweeper;

public partial class LoadSavedGame : LoadScene
{
	[Export(PropertyHint.GlobalSaveFile, "*.json")]
	public string File { get; set; } = "game1.json";

	public void OnPressed()
	{
		var map = ResourceLoader.Load<PackedScene>(Scene).Instantiate<Map>();
		map.Game = Game.Load(new(File));
		ChangeSceneToNode(this, map);
	}
}
