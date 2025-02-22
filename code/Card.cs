using Sandbox;

public sealed class Card : Component, IFlippable
{

	private TextRenderer _rankTR;

	private TextRenderer _suiteTR;

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
					_rankTR = tr;
					tr.Text = _card.Rank;
					tr.Network.Refresh();

				}
				if ( tr.GameObject.Name.Equals( "suite" ) )
				{
					_suiteTR = tr;
					tr.Text = _card.Suite;
					tr.Network.Refresh();
				}
				
			}

			IsFlipped = _card.IsFlipped;
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

	[Sync, Property, ReadOnly]
	public bool IsFlipped
	{
		get
		{
			return _card.IsFlipped;
		}
		set
		{
			_card.IsFlipped = value;
			_rankTR.Enabled = !value;
			_suiteTR.Enabled = !value;
		}
	}

	[Button]
	public void Flip()
	{
		IsFlipped = !IsFlipped;
	}

	public override string ToString()
	{
		return _card.ToString();
	}
}
