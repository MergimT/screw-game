using System.Collections.Generic;

namespace ScrewGame.Presentation
{
    /// <summary>Bundled English/French strings. Missing keys fall back to English, then to the key itself.</summary>
    public static class Loc
    {
        public static string Language = "en";

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            ["title"] = "Screw Workshop",
            ["play"] = "Play",
            ["levels"] = "Levels",
            ["daily"] = "Daily challenge",
            ["daily_done"] = "Daily done - come back tomorrow",
            ["collection"] = "Collection",
            ["settings"] = "Settings",
            ["back"] = "Back",
            ["level"] = "Level {0}",
            ["undo"] = "Undo",
            ["hint"] = "Hint",
            ["restart"] = "Restart",
            ["next"] = "Next",
            ["menu"] = "Menu",
            ["won"] = "Object complete!",
            ["won_body"] = "{0} added to your collection.",
            ["stuck"] = "No moves left",
            ["stuck_body"] = "Undo a move or restart. Restarts are always free.",
            ["watch_ad"] = "Watch ad for +1 help",
            ["ads_unavailable"] = "Rewarded help is unavailable right now.",
            ["ad_granted"] = "+1 help credit",
            ["no_help"] = "No help left this attempt.",
            ["thinking"] = "Looking for a move...",
            ["hint_none"] = "No winning move from here. Try undo or restart.",
            ["hint_unknown"] = "Couldn't find a hint in time. No help was used.",
            ["blocked"] = "That screw is held by another part.",
            ["hidden"] = "Rotate to reach that screw.",
            ["full"] = "Holding area is full.",
            ["save_failed"] = "Couldn't save. Your last move was not applied.",
            ["recovered"] = "Your save was restored from a backup.",
            ["reset_notice"] = "Saved data was unreadable and has been reset.",
            ["readonly_notice"] = "This save came from a newer version. Progress won't be saved.",
            ["replaced_notice"] = "This level was updated, so a new attempt started.",
            ["sound"] = "Sound",
            ["haptics"] = "Haptics",
            ["reduced_motion"] = "Reduced motion",
            ["symbols"] = "Color symbols",
            ["language"] = "Language",
            ["on"] = "On",
            ["off"] = "Off",
            ["locked"] = "Locked",
            ["help_left"] = "{0}",
            ["tut_1"] = "Drag to rotate. Tap a screw to remove it.",
            ["tut_2"] = "Screws go to the tray of their color, or wait in the holding slots.",
            ["tut_3"] = "Parts drop once every screw holding them is out.",
            ["tut_4"] = "Some parts cover others. Clear the top ones first.",
            ["collected"] = "{0} / {1} objects",
            ["privacy"] = "Privacy choices",
        };

        private static readonly Dictionary<string, string> Fr = new Dictionary<string, string>
        {
            ["title"] = "Atelier des vis",
            ["play"] = "Jouer",
            ["levels"] = "Niveaux",
            ["daily"] = "Défi du jour",
            ["daily_done"] = "Défi terminé - revenez demain",
            ["collection"] = "Collection",
            ["settings"] = "Réglages",
            ["back"] = "Retour",
            ["level"] = "Niveau {0}",
            ["undo"] = "Annuler",
            ["hint"] = "Indice",
            ["restart"] = "Recommencer",
            ["next"] = "Suivant",
            ["menu"] = "Menu",
            ["won"] = "Objet terminé !",
            ["won_body"] = "{0} rejoint votre collection.",
            ["stuck"] = "Plus aucun coup",
            ["stuck_body"] = "Annulez un coup ou recommencez. Recommencer est toujours gratuit.",
            ["watch_ad"] = "Pub pour +1 aide",
            ["ads_unavailable"] = "L'aide par publicité est indisponible.",
            ["ad_granted"] = "+1 crédit d'aide",
            ["no_help"] = "Plus d'aide pour cette partie.",
            ["thinking"] = "Recherche d'un coup...",
            ["hint_none"] = "Aucun coup gagnant d'ici. Annulez ou recommencez.",
            ["hint_unknown"] = "Indice introuvable à temps. Aucune aide utilisée.",
            ["blocked"] = "Cette vis est retenue par une autre pièce.",
            ["hidden"] = "Tournez l'objet pour atteindre cette vis.",
            ["full"] = "La zone d'attente est pleine.",
            ["save_failed"] = "Sauvegarde impossible. Le coup n'a pas été appliqué.",
            ["recovered"] = "Votre sauvegarde a été restaurée depuis une copie.",
            ["reset_notice"] = "Les données illisibles ont été réinitialisées.",
            ["readonly_notice"] = "Sauvegarde d'une version plus récente. La progression ne sera pas enregistrée.",
            ["replaced_notice"] = "Ce niveau a été mis à jour : nouvelle partie.",
            ["sound"] = "Son",
            ["haptics"] = "Vibrations",
            ["reduced_motion"] = "Animations réduites",
            ["symbols"] = "Symboles de couleur",
            ["language"] = "Langue",
            ["on"] = "Oui",
            ["off"] = "Non",
            ["locked"] = "Verrouillé",
            ["help_left"] = "{0}",
            ["tut_1"] = "Glissez pour tourner. Touchez une vis pour la retirer.",
            ["tut_2"] = "Les vis vont dans le plateau de leur couleur, ou attendent dans les emplacements.",
            ["tut_3"] = "Une pièce tombe quand toutes ses vis sont retirées.",
            ["tut_4"] = "Certaines pièces en couvrent d'autres. Libérez celles du dessus d'abord.",
            ["collected"] = "{0} / {1} objets",
            ["privacy"] = "Choix de confidentialité",
        };

        public static IEnumerable<string> Keys => En.Keys;
        public static bool HasFrench(string key) => Fr.ContainsKey(key);

        public static string T(string key)
        {
            var table = Language == "fr" ? Fr : En;
            if (table.TryGetValue(key, out var v)) return v;
            return En.TryGetValue(key, out v) ? v : key;
        }

        public static string F(string key, params object[] args) => string.Format(T(key), args);
    }
}
