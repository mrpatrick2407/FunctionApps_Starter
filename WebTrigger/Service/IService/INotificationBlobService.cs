using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTrigger.Service.IService
{
    public interface INotificationBlobService
    {
        BlobClient GetBlobClient(string blobname);
        Task UploadAsync(byte[] imagebytes, BlobClient blobClient);
        Task UploadAsync(Stream imagestream, string blobName);
        Task UploadAsync(byte[] imagebytes, string blobName);
    }
}
