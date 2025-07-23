using Godot;
using System.IO;

public partial class ListSaves : VBoxContainer
{
    [Export]
    public PackedScene LoadButton { get; set; }

    public override void _EnterTree()
    {
        var saves = new DirectoryInfo("saves");
        if (!saves.Exists)
            saves.Create();
        else
            foreach (var file in saves.EnumerateFiles("*.json"))
            {
                var btn = LoadButton.Instantiate<LoadSavedGame>();
                btn.File = file.FullName;
                btn.ButtonName = Path.GetFileNameWithoutExtension(file.Name);
                AddChild(btn);
            }
    }
}
