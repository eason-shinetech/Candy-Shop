using TMPro;
using UnityEngine;

namespace CandyShop
{
    // Collection progress header at the top of the shop list.
    public class SpecialHeader : MonoBehaviour
    {
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _hintText;

        public void Bind(string progress, string hint)
        {
            _progressText.text = progress;
            _hintText.text = hint;
        }
    }
}
