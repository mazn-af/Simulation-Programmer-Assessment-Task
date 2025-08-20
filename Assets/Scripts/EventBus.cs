using System;
public static class EventBus
{
    public static Action OnAlertRaised;
    public static Action OnAlertAccepted;
    public static Action OnMissionSucceeded;

    public static Action OnSprayStarted;
    public static Action OnSprayStopped;

    public static Action<FireNode> OnFireIgnited;
    public static Action<FireNode> OnFireExtinguished;
}
