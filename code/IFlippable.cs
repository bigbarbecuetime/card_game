using Sandbox;

interface IFlippable
{
	public bool IsFlipped { get; set; }
	/// <summary>
	/// Flips the flippable.
	/// </summary>
	public void Flip();
}
