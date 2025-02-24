using Sandbox;
using Sandbox.Diagnostics;
using System;

public sealed class Deck : Component, IFlippable
{
	private TextRenderer RankTR { get; set; }

	private TextRenderer SuiteTR { get; set; }

	public struct CardDesc
	{
		public string Rank { get; init; }
		public string Suite { get; init; }
		public bool IsFlipped { get; set; }

		public CardDesc( string rank, string suite, bool isFlipped)
		{
			Rank = rank;
			Suite = suite;
			IsFlipped = isFlipped;
		}

		public CardDesc( string rank, string suite )
		{
			Rank = rank;
			Suite = suite;
			IsFlipped = false;
		}

		public override string ToString()
		{
			return $"Card {Rank} of {Suite}";
		}
	}

	bool _isFlipped = false;

	[Sync]
	public bool IsFlipped 
	{ 
		get
		{
			return _isFlipped;
		}
		set
		{
			if ( value != _isFlipped )
			{
				if ( CardsLoaded )
				{
					NetList<CardDesc> tmpCards = new NetList<CardDesc>();

					foreach ( CardDesc c in Cards.AsEnumerable().Reverse() )
					{
						tmpCards.Add( new CardDesc( c.Rank, c.Suite, !c.IsFlipped ) );
					}

					Cards = tmpCards;
				}
				else
				{
					_startingDeck.Reverse();


					List<CardDesc> tmpStartDeck = new List<CardDesc>();

					foreach ( CardDesc c in _startingDeck )
					{
						tmpStartDeck.Add( new CardDesc( c.Rank, c.Suite, !c.IsFlipped ) );
					}

					_startingDeck = tmpStartDeck;
				}
				
			}

			_isFlipped = value;

			UpdateText();
		}
	}

	[Sync]
	private bool CardsLoaded { get; set; } = false;

	// HACK: A different card prefab for each card? This is really a stopgap solution
	[Property]
	GameObject CardPrefab {  get; set; }

	[Property, Group( "Deck Content" )]
	private int Size
	{
		get
		{
			return CardsLoaded ? Cards.Count : _startingDeck.Count;
		}
	}

	[Property, Group( "Deck Content" ), HideIf( "CardsLoaded", true )]
	private List<CardDesc> _startingDeck = new List<CardDesc>();

	[Sync]
	private NetList<CardDesc> Cards { get; set; } = new NetList<CardDesc>();

	private bool _sequenceAdd;

	private int _sequence = 0;

	[Property, Group( "Modify Cards" )]
	private bool SequenceAdd
	{
		get
		{
			return _sequenceAdd;
		}
		set
		{
			if ( value )
			{
				SetSequence();	
			}

			_sequenceAdd = value;
		}
	}

	[Property, Group( "Modify Cards" ), ShowIf("SequenceAdd", true)]
	private bool excludeSuite = false;

	private int _startsFrom;

	[Property, Group( "Modify Cards" ), ShowIf( "SequenceAdd", true )]
	private int StartsFrom
	{
		get { return _startsFrom; }
		set
		{
			_startsFrom = value;

			SetSequence();
		}
	}

	[Button, Group( "Modify Cards" ), ShowIf( "SequenceAdd", true )]
	private void ResetSequence() => SetSequence();

	[Property, Group( "Modify Cards" )]
	private string cardRank = "";

	[Property, Group( "Modify Cards" )]
	private string cardSuite = "";

	[Button, Group( "Modify Cards" )]
	private void AddCard()
	{
		CardDesc c = new CardDesc( cardRank, cardSuite );

		if (!CardsLoaded) _startingDeck.Add( c );
		else Cards.Add( c );
		if ( SequenceAdd )
		{
			_sequence++;
			cardRank = $"{_sequence}";
			if ( !excludeSuite ) cardSuite = $"{_sequence}";
		}

		UpdateText();
	}

	[Button, Group( "Modify Cards" )]
	private void DrawTopCard() => DrawTop();

	[Button, Group( "Modify Cards" )]
	private void DrawBottomCard() => DrawBottom();

	/// <summary>
	/// Shuffle the deck using the Fisher-Yates shuffle
	/// </summary>
	[Button, Group( "Modify Cards" )]
	public void Shuffle()
	{
		Random random = new Random();

		if ( CardsLoaded )
		{
			for ( int i = Cards.Count - 1; i >= 1; i-- )
			{
				int rIndex = random.Next( 0, i + 1 );
				(Cards[rIndex], Cards[i]) = (Cards[i], Cards[rIndex]);
			}
		}
		else
		{
			for ( int i = Cards.Count - 1; i >= 1; i-- )
			{
				int rIndex = random.Next( 0, i + 1 );
				(Cards[rIndex], Cards[i]) = (Cards[i], Cards[rIndex]);
			}
		}

		UpdateText();
	}

	[Property, Group( "Modify Cards" ), HideIf( "CardsLoaded", true )]
	private bool ShuffleOnStart { get; set; }

	private void SetSequence()
	{
		cardRank = $"{_startsFrom}";
		if ( !excludeSuite ) cardSuite = $"{_startsFrom}";
		_sequence = _startsFrom;
	}

	/// <summary>
	/// Draw a card from the bottom of the deck.
	/// If the last card of the deck is drawn, the deck is removed
	/// </summary>
	/// <returns></returns>
	public Card DrawBottom() => Draw( 0 );

	/// <summary>
	/// Draw a card from the top of the deck.
	/// If the last card of the deck is drawn, the deck is removed
	/// </summary>
	/// <returns></returns>
	public Card DrawTop() => Draw( Cards.Count-1 );

	/// <summary>
	/// Draws a card from the index, if the last card of the deck is drawn, the deck is removed.
	/// </summary>
	/// <returns></returns>
	private Card Draw(int i)
	{
		Assert.True(Cards.Count > 0, "THE DECK HAS ATTEMPTED TO DRAW WITH NO CARDS");
		
		CardDesc logicalCard = Cards[i];

		if ( CardsLoaded )
		{
			Cards.RemoveAt( i );
		}
		else
		{
			_startingDeck.RemoveAt( i );
		}

		if ( Cards.Count == 0 )
		{
			GameObject.Destroy();
		}
		//Log.Info( $"Removed {logicalCard}" );

		// Create and adjust the position of the prefab
		GameObject cardObject = CardPrefab.Clone();
		cardObject.BreakFromPrefab();
		cardObject.Name = logicalCard.ToString();
		cardObject.LocalPosition = GameObject.LocalPosition;
		cardObject.LocalRotation = GameObject.LocalRotation;

		// Add the card component to the world object
		Card worldCard = cardObject.AddComponent<Card>();
		worldCard.CardValue = logicalCard;

		worldCard.GameObject.NetworkSpawn();

		UpdateText();

		return worldCard;
	}

	[Button, Group( "Modify Cards" )]
	public void Flip() => IsFlipped = !IsFlipped;

	private CardDesc PeekTop() => Peek( Size - 1 );

	private CardDesc Peek(int i)
	{
		return CardsLoaded ? Cards[i] : _startingDeck[i];
	}

	private void UpdateText()
	{
		Log.Info( "Attempting to update text" );
		if ( Size == 0) return;

		RankTR.Text = PeekTop().Rank;
		RankTR.Enabled = !IsFlipped;
		RankTR.Network.Refresh();

		SuiteTR.Text = PeekTop().Suite;
		SuiteTR.Enabled = !IsFlipped;
		SuiteTR.Network.Refresh();
	}

	protected override void OnStart()
	{
		// HACK: Not the best design for this, requires the card to have a text child 
		foreach ( TextRenderer tr in GameObject.GetComponentsInChildren<TextRenderer>() )
		{
			if ( tr.GameObject.Name.Equals( "rank" ) )
			{
				RankTR = tr;
			}
			if ( tr.GameObject.Name.Equals( "suite" ) )
			{
				SuiteTR = tr;
			}

		}
		if ( IsProxy ) return;

		GameObject.BreakFromPrefab();

		foreach ( CardDesc card in _startingDeck )
		{
			Cards.Add( card );
		}

		CardsLoaded = true;

		if (ShuffleOnStart) Shuffle();
		UpdateText();
	}
}
