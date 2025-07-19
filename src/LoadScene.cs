using Godot;

public abstract partial class LoadScene : Node
{
	[Export(PropertyHint.File, "*.tscn")]
	public required string Scene { get; set; }
    [Signal]
    public delegate void LoadingEventHandler(bool isLoading);

	public static void ChangeSceneToNode(Node @this, Node node)
	{
		var tree = @this.GetTree();
		var curScene = tree.CurrentScene;
		tree.Root.AddChild(node);
		tree.Root.RemoveChild(curScene);
		tree.CurrentScene = node;
	}
}
