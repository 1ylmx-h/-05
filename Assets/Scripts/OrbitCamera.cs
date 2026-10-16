using UnityEngine;
using UnityEngine.EventSystems;

namespace GlobalDeformationDemo
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        public Vector3 Target = new Vector3(0f, 1.5f, 0f);
        public float Distance = 6.5f;
        public float Yaw = 30f;
        public float Pitch = 18f;

        private void LateUpdate()
        {
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButton(1) && !overUi)
            {
                Yaw += Input.GetAxis("Mouse X") * 4f;
                Pitch -= Input.GetAxis("Mouse Y") * 4f;
                Pitch = Mathf.Clamp(Pitch, -75f, 80f);
            }

            if (!overUi)
            {
                Distance *= Mathf.Exp(-Input.mouseScrollDelta.y * 0.12f);
                Distance = Mathf.Clamp(Distance, 2f, 18f);
            }

            Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            transform.position = Target + rotation * Vector3.back * Distance;
            transform.rotation = rotation;
        }
    }
}
