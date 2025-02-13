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

	// HACK: A different card prefab for each card? This is really a stopgap solution
	[Property]
	GameObject CardPrefab {  get; set; }

	[Property, Group( "Deck Content" )]
	private int Size
	{
		get
		{
			return _deck.Count;
		}
	}

	[Property, Group( "Deck Content" )]
	private readonly List<CardDesc> _deck = new List<CardDesc>();

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
		_deck.Add( new CardDesc( cardRank, cardSuite ) );
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
		for (int i = _deck.Count-1; i>=1; i--)
		{
			int rIndex = random.Next( 0, i+1 );
			(_deck[rIndex], _deck[i]) = (_deck[i],  _deck[rIndex]);
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
	public Card DrawTop() => Draw( _deck.Count-1 );

	/// <summary>
	/// Draws a card from the index, if the last card of the deck is drawn, the deck is removed.
	/// </summary>
	/// <returns></returns>
	private Card Draw(int i)
	{
		Assert.True(_deck.Count > 0, "THE DECK HAS ATTEMPTED TO DRAW WITH NO CARDS");
		
		CardDesc logicalCard = _deck[i];
		_deck.RemoveAt( i );
		if ( _deck.Count == 0 )
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
		GameObject.BreakFromPrefab();
		Shuffle();
	}
}
