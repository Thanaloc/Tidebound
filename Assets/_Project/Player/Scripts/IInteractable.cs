namespace Player
{
    interface IInteractable 
    {
        bool HoldInteraction { get; }
        string InteractionText { get; }

        void OnInteractionTriggered(PlayerInteraction interact);
        void OnRaycastHitEnter();
        void OnRaycastHitExit();
    }

}
