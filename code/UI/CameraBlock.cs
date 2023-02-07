using Facepunch.Gunfight;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Sandbox;

partial class CameraBlock : Panel
{
	public CameraBlock()
	{
		this.AddClass( "pointer-event" );
	}

	public override void Tick()
	{
		base.Tick();

		MMOPlayer pawn = Game.LocalPawn as MMOPlayer;

		if ( pawn.Focus == MMOPlayer.CameraFocus.FocusMove | pawn.Focus == MMOPlayer.CameraFocus.FocusLook )
		{
			this.RemoveClass( "pointer-event" );
		} else
		{
			this.AddClass( "pointer-event" );
		}
	}
}
