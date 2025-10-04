namespace AccountService.Models
{
    public class ProfileData
    {
        private const string PROFILE_PICTURE_LINK_DEFAULT = "asd";
        public int? Id { get; set; }
        public string? Username { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        public string? ProfilePictureLink { get; set; }
        
        public ProfileData(string username, string name)
        {
            Username = username;
            Name = name;
            ProfilePictureLink = PROFILE_PICTURE_LINK_DEFAULT;
        }

        public ProfileData(string username, string name, string description)
        {
            Username = username;
            Name = name;
            Description = description;
            ProfilePictureLink = PROFILE_PICTURE_LINK_DEFAULT;
        }
         
        public ProfileData() { }
    }
}
