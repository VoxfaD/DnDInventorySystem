using System;
using System.Linq;

namespace DnDInventorySystem.Models
{
    public static class PrivilegeSets
    {
        public const GamePrivilege Owner = GamePrivilege.All;

        public const GamePrivilege Player =
            GamePrivilege.CreateItems |
            GamePrivilege.EditItems |
            GamePrivilege.ViewItems |
            GamePrivilege.CreateCharacters |
            GamePrivilege.EditCharacters |
            GamePrivilege.ViewCharacters |
            GamePrivilege.AddItemsToCharacters |
            GamePrivilege.RemoveItemsFromCharacters |
            GamePrivilege.EditCharacterInventory;

        public static string ToNames(GamePrivilege privileges)
        {
            var list = Enum.GetValues(typeof(GamePrivilege))
                .Cast<GamePrivilege>()
                .Where(p => p != GamePrivilege.None && p != GamePrivilege.All && privileges.HasFlag(p))
                .Select(p => p.ToString());

            var names = string.Join(", ", list);
            if (string.IsNullOrWhiteSpace(names) && privileges == GamePrivilege.All)
            {
                // When All is set but no individual flag is enumerated, expand to every flag.
                names = string.Join(", ", Enum.GetValues(typeof(GamePrivilege))
                    .Cast<GamePrivilege>()
                    .Where(p => p != GamePrivilege.None && p != GamePrivilege.All)
                    .Select(p => p.ToString()));
            }

            return string.IsNullOrWhiteSpace(names) ? "None" : names;
        }
    }
}
