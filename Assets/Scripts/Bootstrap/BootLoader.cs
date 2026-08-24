using UnityEngine;
using UnityEngine.SceneManagement;

namespace CandyShop
{
    // Boot: orientation lock, save load, ad service init, daily sign-in, then Main Menu (spec 10.1).
    public class BootLoader : MonoBehaviour
    {
        private void Awake()
        {
            // Portrait runtime lock; landscape is not supported.
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;

            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            var save = SaveDataService.Load();
            I18nService.Initialize();
            StaminaService.RefreshOnDateRoll(); // stamina refresh rides with boot sign-in (spec 8.2)
            EconomyManager.Init(save, Resources.Load<EconomyConfig>("Data/EconomyConfig"));
            AdServiceLocator.Service = new StubAdService(
                Resources.Load<AdConfig>("Data/AdConfig"), save);

            // Daily sign-in + featured challenge roll happen once per boot.
            var econ = EconomyManager.Config;
            var challengeCfg = Resources.Load<DailyChallengeConfig>("Data/DailyChallengeConfig");
            var catalog = Resources.LoadAll<CandyTypeDefinition>("Data/Catalog");
            var recipes = Resources.LoadAll<RecipeDefinition>("Data/Recipes");
            System.Array.Sort(recipes, (a, b) =>
                a == null || b == null ? 0 : a.cost.CompareTo(b.cost));

            DailySignInService.Evaluate(save, econ, challengeCfg, recipes, catalog);

            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
