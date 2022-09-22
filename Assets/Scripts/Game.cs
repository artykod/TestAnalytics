using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField]
    private EventService _eventService;

    public void TrackLevelStart()
    {
        _eventService.TrackEvent("levelStart", "level:3");
    }

    public void TrackGetReward()
    {
        _eventService.TrackEvent("getReward", "coins:100");
    }

    public void TrackCoinSpent()
    {
        _eventService.TrackEvent("coinSpent", "count:3");
    }
}
