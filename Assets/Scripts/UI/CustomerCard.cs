using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CandyShop
{
    // Prefab-bound view for a queue customer card.
    public class CustomerCard : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private TMP_Text _label;

        public void Bind(string portraitPath, string labelText)
        {
            _portrait.sprite = UIKit.LoadSprite(portraitPath);
            _label.text = labelText;
        }
    }
}
