using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DnDInventorySystem.Models
{
    public class Role
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = "";

        public ICollection<RolePerson> RolePersons { get; set; } = new List<RolePerson>();
    }
}