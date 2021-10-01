using TellahPhotoLibrary.Models;
using TellahPhotoLibrary.Video.Models;

namespace TellahPhotoLibrary.Video
{
    public interface IVideoApi
    {
        double GetVideoDurationSeconds(string file);

        ConvertVideoToMp4Response ConvertVideoToMp4(ConvertVideoToMp4Options options);
        CreateVideoThumbnailResponse CreateVideoThumbnail(CreateVideoThumbnailOptions options);
    }
}
