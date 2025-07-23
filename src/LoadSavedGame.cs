using System;
using System.IO;
using Godot;
using InfiniteMinesweeper;

public partial class LoadSavedGame : LoadScene
{
	[Export(PropertyHint.GlobalSaveFile, "*.json")]
	public string File { get; set; } = "saves/game1.json";
	public string ButtonName
	{
		get;
		set
		{
			field = value;
			EmitSignalButtonNameChanged(ButtonName);
		}
	}

	private bool _activated = false;

	[Signal]
	public delegate void ButtonNameChangedEventHandler(string name);

	public override void _EnterTree()
	{
		ReadOnlySpan<string> args = System.Environment.GetCommandLineArgs();
		GetWindow().FilesDropped += FileDropped;
		if (args.IndexOf("--") is > 0 and var dash && args[(dash + 1)..] is [var path] && new FileInfo(path) is { Exists: true })
		{
			File = path;
			_activated = true;
		}
    }

    private void FileDropped(string[] files)
	{
		if (files is [var path] && new FileInfo(path) is { Exists: true })
		{
			File = path;
			_activated = true;
		}
	}

    public override void _ExitTree()
	=> GetWindow().FilesDropped -= FileDropped;

    public override void _Process(double delta)
	{
		if (_activated)
			OnPressed();
    }

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
