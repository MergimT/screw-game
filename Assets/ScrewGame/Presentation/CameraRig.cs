using ScrewGame.Core;
using UnityEngine;

namespace ScrewGame.Presentation
{
    /// <summary>
    /// Constrained orbit camera around the object. Distance is fitted so the object's bounding sphere fills the free screen
    /// region between the holding area and the bottom bar; an off-centre projection moves the object into that region.
    /// Orientation never affects rules.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Camera;
        public float Yaw = 20f;
        public float Pitch = 35f;
        public Vector3 Target = new Vector3(0f, 0.4f, 0f);
        public float Radius = 3f;
        /// <summary>Viewport heights (0 = bottom) of the free region the object must fit into.</summary>
        public float RegionBottom = 0.13f;
        public float RegionTop = 0.62f;
        private CameraRange _range = new CameraRange();

        public void Configure(CameraRange range, Bounds bounds)
        {
            _range = range ?? new CameraRange();
            Target = bounds.center;
            Radius = Mathf.Max(0.5f, bounds.extents.magnitude);
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
            float aspect = Mathf.Max(0.2f, Camera.aspect);
            float tanV = Mathf.Tan(Camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float regionHalf = Mathf.Max(0.1f, RegionTop - RegionBottom);
            float fit = Radius * 0.78f;
            float distance = Mathf.Max(fit / (tanV * regionHalf), Radius * 0.95f / (tanV * aspect * 0.96f));
            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Camera.transform.position = Target + rot * new Vector3(0f, 0f, -distance);
            Camera.transform.rotation = rot;
            var proj = Matrix4x4.Perspective(Camera.fieldOfView, aspect, Camera.nearClipPlane, Camera.farClipPlane);
            proj[1, 2] = -((RegionBottom + RegionTop) - 1f);
            Camera.projectionMatrix = proj;
        }
    }
}
