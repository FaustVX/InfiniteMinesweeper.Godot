using Godot;

public partial class LoadScene : Button
{
	[Export(PropertyHint.File, "*.tscn")]
	public required string Scene { get; set; }
	[Export]
	public required SpinBox Seed { get; set; }
	[Export]
	public required HSlider MinesPerChunk { get; set; }

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	public override void _Pressed()
	{
		// GetTree().ChangeSceneToFile(Scene);
		if (Scene != null)
		{
			var map = ResourceLoader.Load<PackedScene>(Scene).Instantiate<Map>();
			map.Seed = (int)Seed.Value;
			map.MinesPerChunk = (int)MinesPerChunk.Value;
			ChangeSceneToNode(this, map);
		}
	}

	public static void ChangeSceneToNode(Node @this, Node node)
	{
		var tree = @this.GetTree();
		var curScene = tree.CurrentScene;
		tree.Root.AddChild(node);
		tree.Root.RemoveChild(curScene);
		tree.CurrentScene = node;
	}
}
