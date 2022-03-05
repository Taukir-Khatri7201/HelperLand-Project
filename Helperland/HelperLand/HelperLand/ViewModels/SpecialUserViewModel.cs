namespace HelperLand.ViewModels
{
    public class SpecialUserViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserAvatar { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsFavorite { get; set; }
        public float ratings { get; set; }
        public int cleanings { get; set; }
    }
}
