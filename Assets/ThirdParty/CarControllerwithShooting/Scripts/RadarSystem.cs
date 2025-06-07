using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace CarControllerwithShooting
{
    public class RadarSystem : MonoBehaviour
    {
        public static RadarSystem Instance;

        public Transform PlayerTarget;
        public Transform PlayerCamera;
        public float RadarDistance;
        public Image Background;
        public Transform PlayerView;
        public RectTransform RadarTarget;
        public RectTransform RadarPoint;
        public Transform Root;
        public RadarTypeInfo[] RadarTypeInfo;
        public Dictionary<GameObject, TargetMap> TargetList = new Dictionary<GameObject, TargetMap>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (!PlayerCamera)
                PlayerCamera = CarSystemManager.Instance.GetCamera();
        }

        private void FixedUpdate()
        {
            if (PlayerTarget == null) return;
            transform.eulerAngles = new Vector3(90f, PlayerTarget.eulerAngles.y, 0f);
            RadarDraw();
        }
        public void AddTarget(RadarItem item)
        {
            if (item == null) return;
            AddTarget(item.gameObject, item.TargetType);
        }
        private void AddTarget(GameObject item, RadarTargetType type)
        {
            if (item == null) return;
            if (TargetList.ContainsKey(item)) return;
            var targetInfo = CreateEnemyInfo(type);
            TargetList.Add(item, targetInfo);
        }

        public void RemoveTarget(GameObject item)
        {
            foreach (var target in TargetList)
            {
                if (target.Key != null && target.Key == item)
                {
                    target.Value.TargetPoint.gameObject.SetActive(false);
                    TargetList.Remove(item);
                    break;
                }
            }
        }

        private TargetMap CreateEnemyInfo(RadarTargetType type)
        {
            var enemyInfo = new TargetMap
            {
                TargetPoint = (RectTransform)Instantiate(RadarPoint, new Vector3(0, 0, 0), Quaternion.identity)
            };
            var radarTypeInfo = GetIconInfo(type);
            enemyInfo.TargetPoint.transform.SetParent(Root);
            enemyInfo.TargetPoint.localPosition = new Vector3(0, 0, 0);
            enemyInfo.TargetPoint.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            enemyInfo.TargetPoint.GetComponent<Image>().sprite = radarTypeInfo.Icon;
            return enemyInfo;
        }
        private RadarTypeInfo GetIconInfo(RadarTargetType type)
        {
            for (int i = 0; i < RadarTypeInfo.Length; i++)
            {
                if (RadarTypeInfo[i].Type == type)
                    return RadarTypeInfo[i];
            }

            return RadarTypeInfo[0];
        }

        private void RadarDraw()
        {
            PlayerView.localRotation = Quaternion.AngleAxis(PlayerCamera.eulerAngles.y - PlayerTarget.eulerAngles.y, new Vector3(0, 0, -1));
            GetComponent<Camera>().rect = new Rect(0, 0, 200f / Screen.width, 200f / Screen.height);

            foreach (var enemy in TargetList)
            {
                if(enemy.Key != null)
                {
                    transform.position = new Vector3(PlayerTarget.position.x, enemy.Key.transform.position.y + RadarDistance, PlayerTarget.position.z);
                    if (Vector3.Distance(transform.position, enemy.Key.transform.position) < RadarDistance + RadarDistance * 0.05f)
                    {
                        var screenPos = GetComponent<Camera>().WorldToScreenPoint(enemy.Key.transform.position);
                        var size = Background.rectTransform.sizeDelta.x;
                        screenPos = new Vector3((screenPos.x - size) / 2, (screenPos.y - size) / 2); // anchor back in center
                        enemy.Value.TargetPoint.localPosition = new Vector3(screenPos.x, screenPos.y);

                        enemy.Value.TargetPoint.gameObject.SetActive(true);

                    }
                    else
                    {
                        var olRot = transform.eulerAngles;
                        transform.LookAt(enemy.Key.transform.position);
                        transform.eulerAngles = olRot;
                        enemy.Value.TargetPoint.gameObject.SetActive(false);
                    }
                }
            }
        }

        public void SetPlayer(Transform player)
        {
            transform.parent.gameObject.SetActive(true);
            PlayerTarget = player.transform;
        }

        public void DeletePlayer(Transform player)
        {
            transform.parent.gameObject.SetActive(false);
            PlayerTarget = null;
        }
    }

    public class TargetMap
    {
        public RectTransform TargetPoint;
    }
}
