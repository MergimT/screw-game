using UnityEngine;

namespace ScrewGame.Presentation
{
    public sealed class ScrewView : MonoBehaviour
    {
        public string ScrewId;
        public int Color;
        public Collider Collider;
    }

    public sealed class PartView : MonoBehaviour
    {
        public string PartId;
        public Collider Collider;
    }
}
