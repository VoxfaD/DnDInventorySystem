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
    }
}
