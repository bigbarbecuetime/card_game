using Sandbox;

public sealed class Card : Component
{

	private Deck.CardDesc _card;

	[Sync]
	public Deck.CardDesc CardValue
	{
		set
		{
			_card = value;

			// HACK: Not the best design for this, requires the card to have a text child 
			foreach ( TextRenderer tr in GameObject.GetComponentsInChildren<TextRenderer>() )
			{
				if ( tr.GameObject.Name.Equals( "rank" ) )
				{
					tr.Text = _card.Rank;
					tr.Network.Refresh();

				}
				if ( tr.GameObject.Name.Equals( "suite" ) )
				{
					tr.Text = _card.Suite;
					tr.Network.Refresh();
				}
				
			}
		}
		get {  return _card; }
	}

	[Sync, Property, ReadOnly]
	public string Rank
	{
		get
		{
			return _card.Rank;
		}
	}

	[Sync, Property, ReadOnly]
	public string Suite
	{
		get
		{
			return _card.Suite;
		}
	}

	public override string ToString()
	{
		return _card.ToString();
	}
}
