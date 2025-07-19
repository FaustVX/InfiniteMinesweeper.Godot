using Godot;
using InfiniteMinesweeper;

public partial class LoadSavedGame : LoadScene
{
	[Export(PropertyHint.GlobalSaveFile, "*.json")]
	public string File { get; set; } = "saves/game1.json";

	[Signal]
	public delegate void FileDoNotExistEventHandler(bool value);

    public override void _Process(double delta)
	=> EmitSignalFileDoNotExist(!System.IO.File.Exists(File));


	public void OnPressed()
	{
		try
		{
			EmitSignalLoading(true);
			var map = ResourceLoader.Load<PackedScene>(Scene).Instantiate<Map>();
			using (var stream = System.IO.File.OpenRead(File))
				map.Game = Game.Load(stream);
			ChangeSceneToNode(this, map);
		}
		finally
		{
			EmitSignalLoading(false);
		}
	}
}
