using Godot;

public partial class TemplatedText : Label
{
    [Export]
    public required string Template { get; set; } = "{0}";

    public void SetText(Variant value)
    => Text = string.Format(Template, value);

    public override void _Ready()
    => SetText("");
}
