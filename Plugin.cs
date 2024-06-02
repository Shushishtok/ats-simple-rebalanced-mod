using ATS_API.Helpers;
using ATS_API.Recipes.Builders;
using BepInEx;
using Eremite;
using Eremite.Buildings;
using Eremite.Controller;
using Eremite.Model;
using HarmonyLib;
using System;

namespace SimpleRebalanceMod
{    
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        private Harmony harmony;        

        private void Awake()
        {
            Instance = this;
            harmony = Harmony.CreateAndPatchAll(typeof(Plugin));            

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(MainController), nameof(MainController.InitReferences))]
        [HarmonyPostfix]
        private static void PostSetupMainController()
        {
            bool debugMode = false;

            Instance.Logger.LogInfo("Initializing post Init References in MainController");            

            WorkshopModel clayPit = (WorkshopModel)MB.Settings.GetBuilding(BuildingTypes.Clay_Pit_Workshop.ToName());

            if (debugMode)
            {
                var woodModel = GoodsTypes.Mat_Raw_Wood.ToName().ToGoodModel();
                var woodRef = new GoodRef() { good = woodModel, amount = 1 };
                clayPit.requiredGoods = [woodRef];

                clayPit.allowedTerrains = [FieldType.Sand, FieldType.Grass];
            }

            WorkshopRecipeBuilder clayPitCopperRecipeBuilder = new(PluginInfo.PLUGIN_GUID, "Clay Pit Copper", GoodsTypes.Metal_Copper_Ore, 4, 80, Grade.Two);
            clayPitCopperRecipeBuilder.AddRequiredIngredients((10, GoodsTypes.Water_Clearance_Water));
            clayPitCopperRecipeBuilder.AddTags(TagTypes.Ore_Tag);
            
            foreach (WorkshopRecipeModel workshopRecipeModel in clayPit.recipes)
            {
                workshopRecipeModel.producedGood.amount = 6;
                workshopRecipeModel.productionTime = 45;
            }

            WorkshopRecipeModel clayPitCopperRecipe = clayPitCopperRecipeBuilder.Build();

            Array.Resize(ref SO.Settings.recipes, SO.Settings.recipes.Length + 1);
            SO.Settings.recipes[SO.Settings.recipes.Length - 1] = clayPitCopperRecipe;
            SO.Settings.recipesCache.cache = null;
            
            WorkshopRecipeModel[] recipes = [clayPit.recipes[0], clayPit.recipes[1], clayPitCopperRecipe];
            clayPit.recipes = recipes;

            clayPit.tags = [BuildingTagTypes.Alchemy.ToBuildingTagModel()];
        }       

        [HarmonyPatch(typeof(MainController), nameof(MainController.OnServicesReady))]
        [HarmonyPostfix]
        private static void HookMainControllerSetup()
        {
            // This method will run after game load (Roughly on entering the main menu)
            // At this point a lot of the game's data will be available.
            // Your main entry point to access this data will be `Serviceable.Settings` or `MainController.Instance.Settings`
            Instance.Logger.LogInfo($"Performing game initialization on behalf of {PluginInfo.PLUGIN_GUID}.");
            Instance.Logger.LogInfo($"The game has loaded {MainController.Instance.Settings.effects.Length} effects.");            
        }

        [HarmonyPatch(typeof(MetaController), nameof(MetaController.OnServicesReady))]
        [HarmonyPrefix]
        private static void HookMetaControllerSetup()
        {
            string clayPitName = "Clay Pit Workshop";

            Instance.Logger.LogInfo("adding clay pit to meta state services list");            

            MB.MetaStateService.Content.buildings.Add(clayPitName);
            MB.MetaStateService.Content.essentialBuildings.Add(clayPitName);

            Instance.Logger.LogInfo("clay pit added");
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.StartGame))]
        [HarmonyPostfix]
        private static void HookEveryGameStart()
        {
            // Too difficult to predict when GameController will exist and I can hook observers to it
            // So just use Harmony and save us all some time. This method will run after every game start
            var isNewGame = MB.GameSaveService.IsNewGame();
            Instance.Logger.LogInfo($"Entered a game. Is this a new game: {isNewGame}.");            
        }        
    }
}
