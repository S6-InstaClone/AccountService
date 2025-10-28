namespace AccountService.Dtos
{
    public class UploadProfilePictureDto
    {
        public int Id { get; set; }
        public IFormFile File { get; set; }
    }
}
