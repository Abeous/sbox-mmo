using Sandbox;
using Sandbox.UI;
using static Facepunch.Gunfight.MMOPlayer;

namespace Facepunch.Gunfight;

public partial class PlayerCamera
{
	public float Distance { get; set; } = 130f;
	public virtual void BuildInput (MMOPlayer player) 
	{
		Distance -= (Input.MouseWheel * 6);
	}

	 public virtual void Update( MMOPlayer player )
	{

		if ( player.Focus != MMOPlayer.CameraFocus.FocusNone )
			Camera.Rotation = player.LookInput.ToRotation();

		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );
		Camera.FirstPersonViewer = null;

		Vector3 targetPos;
		var center = player.Position + Vector3.Up * 64;

		var pos = center;
		var rot = Camera.Rotation * Rotation.FromAxis( Vector3.Up, 0 );

		float distance = Distance * player.Scale;
		targetPos = pos;
		targetPos += rot.Backward * distance;

		var tr = Trace.Ray( pos, targetPos )
			.WithAnyTags( "solid" )
			.Ignore( player )
			.Radius( 8 )
			.Run();

		Camera.Position = tr.EndPosition;
	}

}
