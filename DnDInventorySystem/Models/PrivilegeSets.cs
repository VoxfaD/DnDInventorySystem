namespace DnDInventorySystem.Models
{
    public static class PrivilegeSets
    {
        public const GamePrivilege Owner = GamePrivilege.All;

        public const GamePrivilege Player =
            GamePrivilege.ViewCharacters |
            GamePrivilege.ViewItems |
            GamePrivilege.ViewCategories |
            GamePrivilege.CreateCharacters |
            GamePrivilege.EditCharacters |
            GamePrivilege.DeleteCharacters |
            GamePrivilege.CreateItems |
            GamePrivilege.EditItems |
            GamePrivilege.DeleteItems |
            GamePrivilege.CreateCategories |
            GamePrivilege.EditCategories |
            GamePrivilege.DeleteCategories |
            GamePrivilege.AddItemsToCharacters |
            GamePrivilege.RemoveItemsFromCharacters |
            GamePrivilege.EditCharacterInventory;
    }
}
