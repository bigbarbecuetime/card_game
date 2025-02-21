using Sandbox;
using Sandbox.Diagnostics;
using System;

public sealed class Deck : Component
{
	public readonly struct CardDesc
	{
		public string Rank { get; init; }
		public string Suite { get; init; }

		public CardDesc( string rank, string suite)
		{
			Rank = rank;
			Suite = suite;
		}

		public override string ToString()
		{
			return $"Card {Rank} of {Suite}";
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
	private readonly List<CardDesc> _startingDeck = new List<CardDesc>();

	[Sync]
	private NetList<CardDesc> Cards { get; init; } = new NetList<CardDesc>();

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
	}

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
			Log.Info( "Removing From Shared Deck" );
			Cards.RemoveAt( i );
		}
		else
		{
			Log.Info( "Removing From Local Deck" );
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
		return worldCard;
	}

	protected override void OnStart()
	{
		if ( IsProxy ) return;

		GameObject.BreakFromPrefab();

		foreach ( CardDesc card in _startingDeck )
		{
			Cards.Add( card );
		}

		CardsLoaded = true;

		Shuffle();
	}
}
