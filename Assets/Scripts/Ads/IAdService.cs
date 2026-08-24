using System;

namespace CandyShop
{
    // Reserved ad interface. A real network SDK can implement this later without UI rewrites.
    public interface IAdService
    {
        bool IsReady(AdPlacement placement);
        void ShowRewarded(AdPlacement placement, Action<bool> onRewarded);
        void ShowInterstitial(AdPlacement placement, Action onClosed);
    }
}
