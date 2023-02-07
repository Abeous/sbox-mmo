using Sandbox;
using System.ComponentModel;

namespace Facepunch.Gunfight;

public partial class MMOPlayer
{
	public enum CameraFocus
	{
		FocusNone,
		FocusLook,
		FocusMove
	}



	/// <summary>
	/// Should be Input.AnalogMove
	/// </summary>
	[ClientInput] public Vector2 MoveInput { get; protected set; }

	/// <summary>
	/// Normalized accumulation of Input.AnalogLook
	/// </summary>
	[ClientInput] public Angles LookInput { get; protected set; }

	/// <summary>
	/// ?
	/// </summary>
	[ClientInput] public Entity ActiveWeaponInput { get; set; }

	[Net, Predicted]
	public CameraFocus Focus { get; set; }

	/// <summary>
	/// Position a player should be looking from in world space.
	/// </summary>
	[Browsable( false )]
	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	/// <summary>
	/// Position a player should be looking from in local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Vector3 EyeLocalPosition { get; set; }

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity.
	/// </summary>
	[Browsable( false )]
	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity. In local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Rotation EyeLocalRotation { get; set; }

	/// <summary>
	/// Override the aim ray to use the player's eye position and rotation.
	/// </summary>
	public override Ray AimRay => new Ray( EyePosition, EyeRotation.Forward );

	public override void BuildInput()
	{

		MoveInput = Input.AnalogMove;
		if (Focus != CameraFocus.FocusNone)
		{
			var lookInput = (LookInput + Input.AnalogLook).Normal;
			LookInput = lookInput.WithPitch( lookInput.pitch.Clamp( -90f, 90f ) );
		}

			// Since we're a FPS game, let's clamp the player's pitch between -90, and 90.

	}
}
