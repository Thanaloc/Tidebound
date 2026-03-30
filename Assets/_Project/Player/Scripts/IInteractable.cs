namespace Player
{
    interface IInteractable 
    {
        bool HoldInteraction { get; }

        void OnInteractionTriggered(PlayerInteraction interact);
        void OnRaycastHitEnter();
        void OnRaycastHitExit();
    }

}
