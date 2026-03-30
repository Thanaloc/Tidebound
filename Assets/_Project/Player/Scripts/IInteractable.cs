namespace Player
{
    interface IInteractable 
    {
        bool HoldInteraction { get; }

        void OnInteractionTriggered();
        void OnRaycastHitEnter();
        void OnRaycastHitExit();
    }

}
