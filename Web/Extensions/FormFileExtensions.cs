using Domain.DTO.Requests;

namespace Web.Extensions;

public static class FormFileExtensions
{
    public static FileUploadRequest ToFileUploadRequest(this IFormFile file)
    {
        return new FileUploadRequest
        {
            Content = file.OpenReadStream(),
            ContentType = file.ContentType,
            FileName = file.FileName,
            Length = file.Length
        };
    }
}