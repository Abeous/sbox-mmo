using Facepunch.Gunfight.Mechanics;
using Sandbox;
using System.Linq;

namespace Facepunch.Gunfight;

public partial class MMOPlayer : AnimatedEntity
{
	/// <summary>
	/// The controller is responsible for player movement and setting up EyePosition / EyeRotation.
	/// </summary>
	[BindComponent] public PlayerController Controller { get; }

	/// <summary>
	/// The animator is responsible for animating the player's current model.
	/// </summary>
	[BindComponent] public PlayerAnimator Animator { get; }

	/// <summary>
	/// A camera is known only to the local client. This cannot be used on the server.
	/// </summary>
	public PlayerCamera PlayerCamera { get; protected set; }

	/// <summary>
	/// How long since the player last played a footstep sound.
	/// </summary>
	TimeSince TimeSinceFootstep = 0;

	public bool Autorun { get; private set; } = false;

	/// <summary>
	/// A cached model used for all players.
	/// </summary>
	public static Model PlayerModel = Model.Load( "models/citizen/citizen.vmdl" );

	/// <summary>
	/// When the player is first created. This isn't called when a player respawns.
	/// </summary>
	public override void Spawn()
	{
		Model = PlayerModel;
		Predictable = true;

		// Default properties
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		EnableLagCompensation = true;
		EnableHitboxes = true;

		Focus = CameraFocus.FocusNone;

		Tags.Add( "player" );
	}

	/// <summary>
	/// Called when a player respawns, think of this as a soft spawn - we're only reinitializing transient data here.
	/// </summary>
	public void Respawn()
	{
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -16, -16, 0 ), new Vector3( 16, 16, 72 ) );

		Health = 100;
		LifeState = LifeState.Alive;
		EnableAllCollisions = true;
		EnableDrawing = true;

		//fix spawn in rotation bug
		EyeRotation = Rotation;

		Focus = CameraFocus.FocusNone;

		// Re-enable all children.
		Children.OfType<ModelEntity>()
			.ToList()
			.ForEach( x => x.EnableDrawing = true );

		Components.Create<PlayerController>();

		// Remove old mechanics.
		Components.RemoveAny<PlayerControllerMechanic>();

		// Add mechanics.
		Components.Create<WalkMechanic>();
		Components.Create<JumpMechanic>();
		Components.Create<AirMoveMechanic>();
		Components.Create<SprintMechanic>();
		Components.Create<CrouchMechanic>();
		Components.Create<InteractionMechanic>();

		Components.Create<PlayerAnimator>();

		GameManager.Current?.MoveToSpawnpoint( this );
		ResetInterpolation();

		ClientRespawn( To.Single( Client ) );

		UpdateClothes();
	}

	/// <summary>
	/// Called clientside when the player respawns. Useful for adding components like the camera.
	/// </summary>
	[ClientRpc]
	public void ClientRespawn()
	{
		PlayerCamera = new PlayerCamera();
	}

	/// <summary>
	/// Called every server and client tick.
	/// </summary>
	/// <param name="cl"></param>
	public override void Simulate( IClient cl )
	{
		CheckCameraFocus();

		//Rotation = LookInput.WithPitch( 0f ).ToRotation();
		Rotation = (Focus == CameraFocus.FocusMove) ? LookInput.WithPitch( 0f ).ToRotation() : Rotation;

		Controller?.Simulate( cl );
		Animator?.Simulate( cl );

	}

	/// <summary>
	/// Called every frame clientside.
	/// </summary>
	/// <param name="cl"></param>
	public override void FrameSimulate( IClient cl )
	{
		CheckCameraFocus();

		Controller?.FrameSimulate( cl );
		Animator?.FrameSimulate( cl );

		PlayerCamera?.Update( this );
	}

	[ClientRpc]
	public void SetAudioEffect( string effectName, float strength, float velocity = 20f, float fadeOut = 4f )
	{
		Audio.SetEffect( effectName, strength, velocity: 20.0f, fadeOut: 4.0f * strength );
	}

	private async void AsyncRespawn()
	{
		await GameTask.DelaySeconds( 3f );
		Respawn();
	}

	public override void OnKilled()
	{
		if ( LifeState == LifeState.Alive )
		{
			LifeState = LifeState.Dead;
			EnableAllCollisions = false;
			EnableDrawing = false;

			Controller.Remove();
			Animator.Remove();

			// Disable all children as well.
			Children.OfType<ModelEntity>()
				.ToList()
				.ForEach( x => x.EnableDrawing = false );

			AsyncRespawn();
		}
	}

	/// <summary>
	/// Called clientside every time we fire the footstep anim event.
	/// </summary>
	public override void OnAnimEventFootstep( Vector3 pos, int foot, float volume )
	{
		if ( !Game.IsClient )
			return;

		if ( LifeState != LifeState.Alive )
			return;

		if ( TimeSinceFootstep < 0.2f )
			return;

		volume *= GetFootstepVolume();

		TimeSinceFootstep = 0;

		var tr = Trace.Ray( pos, pos + Vector3.Down * 20 )
			.Radius( 1 )
			.Ignore( this )
			.Run();

		if ( !tr.Hit ) return;

		tr.Surface.DoFootstep( this, tr, foot, volume );
	}

	protected float GetFootstepVolume()
	{
		return Controller.Velocity.WithZ( 0 ).Length.LerpInverse( 0.0f, 200.0f ) * 1f;
	}

	[ConCmd.Server( "kill" )]
	public static void DoSuicide()
	{
		(ConsoleSystem.Caller.Pawn as MMOPlayer)?.TakeDamage( DamageInfo.Generic( 1000f ) );
	}

	[ConCmd.Server( "sethp" )]
	public static void SetHP( float value )
	{
		(ConsoleSystem.Caller.Pawn as MMOPlayer).Health = value;
	}

	private void CheckCameraFocus()
	{
		//if right click is down and left click is not
		if ( Input.Down( InputButton.SecondaryAttack ) && !Input.Down( InputButton.PrimaryAttack ) )
		{
			Focus = CameraFocus.FocusMove;
			Autorun = false;
		}
		//if left click is down and right click is not
		if ( Input.Down( InputButton.PrimaryAttack ) && !Input.Down( InputButton.SecondaryAttack ) )
		{
			Focus = CameraFocus.FocusLook;
			Autorun = false;
		}
		//if both left and right click are down
		if ( Input.Down( InputButton.PrimaryAttack ) && Input.Down( InputButton.SecondaryAttack ) )
		{
			Focus = CameraFocus.FocusMove;
			Autorun = true;
		}
		//if right click is released and left click is not down
		if ( Input.Released( InputButton.SecondaryAttack ) && !Input.Down( InputButton.PrimaryAttack ) )
		{
			Focus = CameraFocus.FocusNone;
			Autorun = false;
		}
		//if left click is released and right click is not down
		if ( Input.Released( InputButton.PrimaryAttack ) && !Input.Down( InputButton.SecondaryAttack) )
		{
			Focus = CameraFocus.FocusNone;
			Autorun = false;
		}

	}
}
