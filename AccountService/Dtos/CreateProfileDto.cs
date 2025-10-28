using System.ComponentModel.DataAnnotations;

namespace AccountService.Dtos
{
    public class CreateProfileDto
    {
        public CreateProfileDto(string? username, string? name, string? description)
        {
            Username = username;
            Name = name;
            Description = description;
        }

        [Required]
        public string? Username { get; set; }
        [Required]
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}