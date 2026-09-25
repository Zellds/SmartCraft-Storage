using System.Collections.Generic;
using Jotunn.Managers;

namespace SmartCraftStorage.Translations
{
    internal static class ModTranslations
    {
        public static void Setup()
        {
            var localization = LocalizationManager.Instance.GetLocalization();

            localization.AddTranslation("English", new Dictionary<string, string>
            {
                ["smartcraft_no_chest_nearby"] = "No chest nearby.",
                ["smartcraft_quickstack_success"] = "Stashed $1 item(s) into nearby chests.",
                ["smartcraft_quickstack_nothing"] = "Nothing to stash into nearby chests.",
                ["smartcraft_restock_no_items_marked"] = "No items marked for restock.",
                ["smartcraft_restock_success"] = "Restocked $1 item(s).",
                ["smartcraft_restock_nothing"] = "Nothing to restock from nearby chests."
            });

            localization.AddTranslation("French", new Dictionary<string, string>
            {
                ["smartcraft_no_chest_nearby"] = "Aucun coffre à proximité.",
                ["smartcraft_quickstack_success"] = "$1 objet(s) rangé(s) dans les coffres à proximité.",
                ["smartcraft_quickstack_nothing"] = "Rien à ranger dans les coffres à proximité.",
                ["smartcraft_restock_no_items_marked"] = "Aucun objet marqué pour le réapprovisionnement.",
                ["smartcraft_restock_success"] = "$1 objet(s) réapprovisionné(s).",
                ["smartcraft_restock_nothing"] = "Rien à réapprovisionner depuis les coffres à proximité."
            });

            localization.AddTranslation("Portuguese_Brazilian", new Dictionary<string, string>
            {
                ["smartcraft_no_chest_nearby"] = "Nenhum baú próximo.",
                ["smartcraft_quickstack_success"] = "Guardado(s) $1 item(ns) nos baús próximos.",
                ["smartcraft_quickstack_nothing"] = "Nada pra guardar nos baús próximos.",
                ["smartcraft_restock_no_items_marked"] = "Nenhum item marcado pra restock.",
                ["smartcraft_restock_success"] = "Restock: $1 item(ns) repostos.",
                ["smartcraft_restock_nothing"] = "Nada pra restockar nos baús próximos."
            });

            localization.AddTranslation("Spanish", new Dictionary<string, string>
            {
                ["smartcraft_no_chest_nearby"] = "No hay ningún cofre cerca.",
                ["smartcraft_quickstack_success"] = "Se guardaron $1 objeto(s) en cofres cercanos.",
                ["smartcraft_quickstack_nothing"] = "No hay nada que guardar en cofres cercanos.",
                ["smartcraft_restock_no_items_marked"] = "No hay objetos marcados para reabastecer.",
                ["smartcraft_restock_success"] = "Reabastecido(s) $1 objeto(s).",
                ["smartcraft_restock_nothing"] = "No hay nada que reabastecer desde cofres cercanos."
            });
        }
    }
}
