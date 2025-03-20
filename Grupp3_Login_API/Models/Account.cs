using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace Grupp3_Login_API.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; } // Detta ska vi sen hasha!
        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public Role Role { get; set; }
    }
}
