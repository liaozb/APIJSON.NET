namespace APIJSON.NET.Models
{
    public class RoleItem
    {
        public string[] Table { get; set; }
        public string[] Column { get; set; }
        public string[] Filter { get; set; }
    }
    public class Role
    {
        public string Name { get; set; }
        public RoleItem Select { get; set; }
        public RoleItem Update { get; set; }
        public RoleItem Insert { get; set; }
        public RoleItem Delete { get; set; }

    }
    
}
