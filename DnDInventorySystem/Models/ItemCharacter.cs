namespace DnDInventorySystem.Models
{
    // Junction table: Character inventory entries
    public class ItemCharacter
    {
        public int Id { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; }

        public int CharacterId { get; set; }
        public Character Character { get; set; }

        public int Quantity { get; set; } = 1;

        // True = equipped/with character; False = stored (stash, chest, etc.)
        public bool IsEquipped { get; set; } = false;
    }
}
