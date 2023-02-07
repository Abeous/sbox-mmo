using Sandbox;
using Sandbox.Diagnostics;
using System.Linq;

namespace Facepunch.Gunfight;

public partial class MMOPlayer
{
	static string realm = Game.IsServer ? "server" : "client";
	static Logger eventLogger = new Logger( $"player/GameEvent/{realm}" );

	public void RunGameEvent( string eventName )
	{
		eventName = eventName.ToLowerInvariant();

		Controller.Mechanics.ToList()
			.ForEach( x => x.OnGameEvent( eventName ) );

		eventLogger.Trace( $"OnGameEvent ({eventName})" );
	}
}
