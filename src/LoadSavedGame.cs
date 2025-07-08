using Godot;
using InfiniteMinesweeper;

public partial class LoadSavedGame : LoadScene
{
	[Export(PropertyHint.GlobalSaveFile, "*.json")]
	public string File { get; set; } = "saves/game1.json";

	public void OnPressed()
	{
		var map = ResourceLoader.Load<PackedScene>(Scene).Instantiate<Map>();
		using (var stream = System.IO.File.OpenRead(File))
			map.Game = Game.Load(stream);
		ChangeSceneToNode(this, map);
	}
}
