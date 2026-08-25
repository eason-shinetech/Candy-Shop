using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CandyShop
{
    // Prefab-bound views for dynamically instantiated UI items. Each one only
    // binds data to serialized child references; layout lives in its prefab.
    public class OrderChip : MonoBehaviour
    {
        [SerializeField] private Image _candyIcon;
        [SerializeField] private TMP_Text _countText;

        public string TypeId { get; private set; }

        public void Bind(CandyTypeDefinition type, int remaining)
        {
            TypeId = type != null ? type.typeId : "";
            _candyIcon.sprite = UIKit.CandyIcon(type);
            string displayName = type != null ? type.LocalizedName : "";
            _countText.text = displayName + " x" + remaining;
        }

        public void SetCount(string displayName, int remaining)
        {
            _countText.text = displayName + " x" + remaining;
        }
    }
}
