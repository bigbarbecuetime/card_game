using Sandbox;

public sealed class CardPlayerController : Component
{
	[Property, Group( "Inputs" ), InputAction]
	string grab = null;

	[Property, Group( "Inputs" ), InputAction]
	string flip = null;


	[Property, ReadOnly]
	private Card held = null;

	private Vector3 holdOffset = Vector3.Zero;

	protected override void OnStart()
	{
		if ( IsProxy ) return;

		Mouse.Visible = true;
	}
	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		// When first pressing down the key
		if ( Input.Pressed(grab) )
		{
			//Log.Info( "Pressed!" );

			//Vector3 pos = Scene.Camera.ScreenToWorld( Mouse.Position );
			SceneTraceResult ray = Scene.Trace.Ray( Scene.Camera.ScreenPixelToRay( Mouse.Position ), 200 ).Run();
			//Vector3 pos = ray.EndPosition;
			// Find world position of where the mouse is clicking
			//Log.Info( $"Screen Position {Mouse.Position}" );
			//Log.Info( $"World Position {pos} " );

			// If the ray hit something
			if (ray.GameObject is GameObject go)
			{
				//Log.Info( $"{go.Name} Clicked On" );
				// If it is a card, grab it
				if (go.GetComponent<Card>() is Card c )
				{
					//Log.Info( "Card Grabbed" );
					go.Network.TakeOwnership();
					held = c;
				}
				// Why in parent? Just in case the collider is on a subpart of the object
				else if (go.GetComponent<Deck>() is Deck d)
				{
					//Log.Info( "Deck Clicked" );
					go.Network.TakeOwnership();
					held = d.DrawTop();
				}

				// The offset to remember when updating the cards position, this is where the card is grabbed from
				if ( held != null )
				{
					holdOffset = held.WorldPosition - Scene.Camera.ScreenToWorld( Mouse.Position ).WithZ( held.LocalPosition.z );
				}
			}
		}
		// While it is held down
		else if (Input.Down(grab))
		{
			// Update the held cards position, and apply the offset for where it is being held from
			// If we do not own the held, this will not happen
			if ( held != null ) held.WorldPosition = holdOffset + Scene.Camera.ScreenToWorld( Mouse.Position ).WithZ( held.LocalPosition.z );
		}
		else if (Input.Released(grab))
		{
			// TODO: If the card being released is touching a deck, add that card to the deck
			held = null;
		}

		if (Input.Pressed(flip))
		{
			if ( held != null )
			{
				held.Flip();
			}
			else
			{
				// Attempt to find a card and flip it
				SceneTraceResult ray = Scene.Trace.Ray( Scene.Camera.ScreenPixelToRay( Mouse.Position ), 200 ).Run();
				// If the ray hit something
				if ( ray.GameObject is GameObject go )
				{
					// If it is a card, flip it
					if ( go.GetComponent<IFlippable>() is IFlippable f )
					{
						go.Network.TakeOwnership();
						f.Flip();
					}

				}
			}
		}
	}
}
