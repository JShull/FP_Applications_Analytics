namespace FuzzPhyte.Applications.Analytics.Sample
{
    using FuzzPhyte.XR;
    using UnityEngine;

    /// <summary>
    /// Test script to invoke fake interaction
    /// </summary>
    public class AAExampleInteract : MonoBehaviour
    {
        public FPWorldItem TheItem;
        public FPTypingText TheLabelDetails;

#if UNITY_EDITOR
        [ContextMenu("Example Pickup Item")]
        public void PickupItemExample()
        {
            TheItem.PickedUpItem(0);
        }
        [ContextMenu("Example Ray Select Item")]
        public void RaySelectItemExample()
        {
            TheItem.RayInteracted(0);
        }
        [ContextMenu("Example Pick up and Label")]
        public void PickUpItemLabel()
        {
            TheItem.PickedUpItem(0);
            TheItem.ActivateDetailedLabelTimer(5);
            TheLabelDetails.StartTypingEffectVocabEnglish();
        }
#endif
    }
}
