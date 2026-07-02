namespace TLN.Application.Feedback
{
	public enum FeedbackEventId
	{
		None = 0,

		ItemPickedUp = 100,
		ItemUsed = 110,

		CampfireFuelAdded = 200,
		CampfireIgnited = 210,
		CampfireExtinguished = 220,

		BuildPlaced = 300,
		BuildFailed = 310,

		PlayerDamaged = 400,
		PlayerFreezing = 410
	}
}
