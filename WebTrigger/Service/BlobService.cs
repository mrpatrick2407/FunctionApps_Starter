using Azure.Storage.Blobs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebTrigger.Model;
using WebTrigger.Service.IService;
using static WebTrigger.Model.ImageModel;

namespace WebTrigger.Service
{
    public class BlobService: IImageBlobService,IImageSmallBlobService,IImageMediumBlobService,INotificationBlobService
    {
        public readonly BlobContainerClient _blobContainerClient;
        public BlobService(string connectionString,string container) {
            _blobContainerClient= new BlobContainerClient(connectionString, container);
            _blobContainerClient.CreateIfNotExistsAsync().Wait();
        }

        public BlobClient GetBlobClient(string blobname)
        {
            
           return _blobContainerClient.GetBlobClient(blobname);
        }

        public async Task ResizeImageAndSaveAsync(Stream input, ImageSize size, IImageFormat Format,string blobName)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            input.Position = 0;
            using var image = Image.Load<Rgba32>(input);

            image.Mutate(x => x.Resize(imageDimensionsTable.GetValueOrDefault(size).Item1, imageDimensionsTable.GetValueOrDefault(size).Item2));
            var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, Format);

            outputStream.Position = 0;
            await UploadAsync(outputStream,blobClient);
            outputStream.Position = 0;          
        }
        public async Task UploadAsync(byte[] imagebytes,BlobClient client)
        {
            using var memstream = new MemoryStream(imagebytes);
            await client.UploadAsync(memstream);
        }public async Task UploadAsync(byte[] imagebytes,string blobName)
        {
            using var memstream = new MemoryStream(imagebytes);
            await GetBlobClient(blobName).UploadAsync(memstream);
        }
        public async Task UploadAsync(Stream imagestream, BlobClient client)
        {           
            await client.UploadAsync(imagestream);
        } 
        public async Task UploadAsync(Stream imagestream, string blobName)
        {           
            await GetBlobClient(blobName).UploadAsync(imagestream);
        }
       
    }
}
