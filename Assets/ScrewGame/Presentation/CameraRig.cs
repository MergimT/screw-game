using ScrewGame.Core;
using UnityEngine;

namespace ScrewGame.Presentation
{
    /// <summary>Constrained orbit camera around the object. Orientation never affects rules.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Camera;
        public float Yaw = 20f;
        public float Pitch = 35f;
        public float Distance = 9f;
        public Vector3 Target = new Vector3(0f, 0.4f, 0f);
        private CameraRange _range = new CameraRange();

        public void Configure(CameraRange range)
        {
            _range = range ?? new CameraRange();
            Distance = _range.Distance;
            Yaw = Mathf.Clamp(20f, _range.MinYaw, _range.MaxYaw);
            Pitch = Mathf.Clamp(35f, _range.MinPitch, _range.MaxPitch);
            Apply();
        }

        public void Rotate(Vector2 deltaDegrees)
        {
            Yaw += deltaDegrees.x;
            if (_range.MaxYaw - _range.MinYaw < 360f) Yaw = Mathf.Clamp(Yaw, _range.MinYaw, _range.MaxYaw);
            else Yaw = Mathf.Repeat(Yaw + 180f, 360f) - 180f;
            Pitch = Mathf.Clamp(Pitch - deltaDegrees.y, _range.MinPitch, _range.MaxPitch);
            Apply();
        }

        public void Apply()
        {
            if (Camera == null) return;
            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            float aspect = Mathf.Max(0.3f, Camera.aspect);
            float portraitBoost = aspect < 1f ? Mathf.Lerp(1.35f, 1f, aspect) : 1f;
            Camera.transform.position = Target + rot * new Vector3(0f, 0f, -Distance * portraitBoost);
            Camera.transform.rotation = rot;
        }
    }
}
