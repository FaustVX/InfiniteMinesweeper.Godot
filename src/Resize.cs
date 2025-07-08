using Godot;

public partial class Resize : Control
{
	public void ResizeControl(float scale)
	{
		var anchorLeft = AnchorLeft;
		var anchorTop = AnchorTop;
		var anchorRight = AnchorRight;
		var anchorBottom = AnchorBottom;
		Scale *= scale;
		AnchorLeft = anchorLeft;
		AnchorTop = anchorTop;
		AnchorRight = anchorRight;
		AnchorBottom = anchorBottom;
	}
}
