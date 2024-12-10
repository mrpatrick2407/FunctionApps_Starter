using Azure.Storage.Blobs;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WebTrigger.Model.ImageModel;

namespace WebTrigger.Service.IService
{
    public interface IImageBlobService : IImageService
    {
    }
    public interface IImageSmallBlobService: IImageService
    {
    }
    public interface IImageMediumBlobService: IImageService
    {
    }
    public interface IImageService
    {
        BlobClient GetBlobClient(string blobname);
        Task UploadAsync(byte[] imagebytes,BlobClient blobClient);
        Task ResizeImageAndSaveAsync(Stream input, ImageSize size, IImageFormat Format, string blobname);
    }
}
