using Godot;

public partial class LoadMainMenu : LoadScene
{
    public void OnPressed()
    {
        var map = ResourceLoader.Load<PackedScene>(Scene).Instantiate();
        ChangeSceneToNode(this, map);
    }
}
