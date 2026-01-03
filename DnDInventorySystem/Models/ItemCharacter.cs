using System.ComponentModel.DataAnnotations.Schema;

namespace DnDInventorySystem.Models
{
    // Junction table: Character inventory entries
    [Table("InventarsTels")]
    public class ItemCharacter
    {
        public int Id { get; set; }

        [Column("InventarsID")]
        public int ItemId { get; set; }
        public Item Item { get; set; }

        [Column("TelsID")]
        public int CharacterId { get; set; }
        public Character Character { get; set; }

        [Column("Daudzums")]
        public int Quantity { get; set; } = 1;

        // True = equipped/with character; False = stored 
        [Column("IrLidzi")]
        public bool IsEquipped { get; set; } = false;
    }
}
