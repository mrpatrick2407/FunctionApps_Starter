using Azure;
using Azure.AI.Vision.Face;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace WebTrigger.Function
{
    public class Cognitive
    {
        private readonly ILogger<Cognitive> _logger;
        static readonly string LargePersonGroupId = Guid.NewGuid().ToString();
        static readonly string SubscriptionKey = Environment.GetEnvironmentVariable("FACE_API_KEY") ?? "<apikey>";
        static readonly string Endpoint = Environment.GetEnvironmentVariable("FACE_API_ENDPOINT") ?? "<endpoint>";
        public Cognitive(ILogger<Cognitive> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Cognitive))]
        public async Task Run([BlobTrigger("images/{name}", Source = BlobTriggerSource.EventGrid, Connection = "AzureWebJobsStorage")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            _logger.LogInformation($"C# Blob Trigger (using Event Grid) processed blob\n Name: {name} \n Data: {content}");
        }

        [Function("Cognition")]
        public async Task RecogniseFace([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            const string ImageBaseUrl = "https://raw.githubusercontent.com/Azure-Samples/cognitive-services-sample-data-files/master/Face/images/";
            FaceRecognitionModel model = FaceRecognitionModel.Recognition04;
            FaceClient client = Authenticate(Endpoint, SubscriptionKey);
            IdentifyInLargePersonGroup(client, ImageBaseUrl, model).Wait();
        }
        public static FaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new Uri(endpoint), new AzureKeyCredential(key));
        }
        private static async Task<List<FaceDetectionResult>> DetectFaceRecognize(FaceClient faceClient, string url, FaceRecognitionModel recognitionModel)
        {
            // Detect faces from image URL.
            var response = await faceClient.DetectAsync(new Uri(url), FaceDetectionModel.Detection03, recognitionModel, true, [FaceAttributeType.QualityForRecognition]);
            IReadOnlyList<FaceDetectionResult> detectedFaces = response.Value;
            List<FaceDetectionResult> sufficientQualityFaces = new List<FaceDetectionResult>();
            foreach (FaceDetectionResult detectedFace in detectedFaces)
            {
                QualityForRecognition? faceQualityForRecognition = detectedFace.FaceAttributes.QualityForRecognition;
                if (faceQualityForRecognition.HasValue && faceQualityForRecognition.Value != QualityForRecognition.Low)
                {
                    sufficientQualityFaces.Add(detectedFace);
                }
            }
            Console.WriteLine($"{detectedFaces.Count} face(s) with {sufficientQualityFaces.Count} having sufficient quality for recognition detected from image `{Path.GetFileName(url)}`");

            return sufficientQualityFaces;
        }

        public static async Task IdentifyInLargePersonGroup(FaceClient client, string url, FaceRecognitionModel recognitionModel)
        {

            // Create a dictionary for all your images, grouping similar ones under the same key.
            Dictionary<string, string[]> personDictionary =
                new Dictionary<string, string[]>
                    { { "Family1-Dad", new[] { "Family1-Dad1.jpg", "Family1-Dad2.jpg" } },
                      { "Family1-Mom", new[] { "Family1-Mom1.jpg", "Family1-Mom2.jpg" } },
                      { "Family1-Son", new[] { "Family1-Son1.jpg", "Family1-Son2.jpg" } }
                    };
            // A group photo that includes some of the persons you seek to identify from your dictionary.
            string sourceImageFileName = "identification1.jpg";

            // Create a large person group.
            Console.WriteLine($"Create a person group ({LargePersonGroupId}).");
            LargePersonGroupClient largePersonGroupClient = new FaceAdministrationClient(new Uri(Endpoint), new AzureKeyCredential(SubscriptionKey)).GetLargePersonGroupClient(LargePersonGroupId);
            await largePersonGroupClient.CreateAsync(LargePersonGroupId, recognitionModel: recognitionModel);
            // The similar faces will be grouped into a single large person group person.
            foreach (string groupedFace in personDictionary.Keys)
            {
                // Limit TPS
                await Task.Delay(250);
                var createPersonResponse = await largePersonGroupClient.CreatePersonAsync(groupedFace);
                Guid personId = createPersonResponse.Value.PersonId;
                Console.WriteLine($"Create a person group person '{groupedFace}'.");

                // Add face to the large person group person.
                foreach (string similarImage in personDictionary[groupedFace])
                {
                    Console.WriteLine($"Check whether image is of sufficient quality for recognition");
                    var detectResponse = await client.DetectAsync(new Uri($"{url}{similarImage}"), FaceDetectionModel.Detection03, recognitionModel, false);
                    IReadOnlyList<FaceDetectionResult> facesInImage = detectResponse.Value;
                    bool sufficientQuality = true;
                    foreach (FaceDetectionResult face in facesInImage)
                    {
                        QualityForRecognition? faceQualityForRecognition = face.FaceAttributes.QualityForRecognition;
                        //  Only "high" quality images are recommended for person enrollment
                        if (faceQualityForRecognition.HasValue && faceQualityForRecognition.Value != QualityForRecognition.High)
                        {
                            sufficientQuality = false;
                            break;
                        }
                    }

                    if (!sufficientQuality)
                    {
                        continue;
                    }

                    if (facesInImage.Count != 1)
                    {
                        continue;
                    }

                    // add face to the large person group
                    Console.WriteLine($"Add face to the person group person({groupedFace}) from image `{similarImage}`");
                    await largePersonGroupClient.AddFaceAsync(personId, new Uri($"{url}{similarImage}"), detectionModel: FaceDetectionModel.Detection03);
                }
            }

            // Start to train the large person group.
            Console.WriteLine();
            Console.WriteLine($"Train person group {LargePersonGroupId}.");
            Operation operation = await largePersonGroupClient.TrainAsync(WaitUntil.Completed);

            // Wait until the training is completed.
            await operation.WaitForCompletionResponseAsync();
            Console.WriteLine("Training status: succeeded.");
            Console.WriteLine();

            Console.WriteLine("Pausing for 60 seconds to avoid triggering rate limit on free account...");
            await Task.Delay(60000);

            List<Guid> sourceFaceIds = new List<Guid>();
            // Detect faces from source image url.
            List<FaceDetectionResult> detectedFaces = await DetectFaceRecognize(client, $"{url}{sourceImageFileName}", recognitionModel);

            // Add detected faceId to sourceFaceIds.
            foreach (FaceDetectionResult detectedFace in detectedFaces) { sourceFaceIds.Add(detectedFace.FaceId.Value); }

            // Identify the faces in a large person group.
            var identifyResponse = await client.IdentifyFromLargePersonGroupAsync(sourceFaceIds, LargePersonGroupId);
            IReadOnlyList<FaceIdentificationResult> identifyResults = identifyResponse.Value;
            foreach (FaceIdentificationResult identifyResult in identifyResults)
            {
                if (identifyResult.Candidates.Count == 0)
                {
                    Console.WriteLine($"No person is identified for the face in: {sourceImageFileName} - {identifyResult.FaceId},");
                    continue;
                }

                FaceIdentificationCandidate candidate = identifyResult.Candidates.First();
                var getPersonResponse = await largePersonGroupClient.GetPersonAsync(candidate.PersonId);
                string personName = getPersonResponse.Value.Name;
                Console.WriteLine($"Person '{personName}' is identified for the face in: {sourceImageFileName} - {identifyResult.FaceId}," + $" confidence: {candidate.Confidence}.");

                var verifyResponse = await client.VerifyFromLargePersonGroupAsync(identifyResult.FaceId, LargePersonGroupId, candidate.PersonId);
                FaceVerificationResult verifyResult = verifyResponse.Value;
                Console.WriteLine($"Verification result: is a match? {verifyResult.IsIdentical}. confidence: {verifyResult.Confidence}");
            }
            Console.WriteLine();

            // Delete large person group.
            Console.WriteLine("========DELETE PERSON GROUP========");
            Console.WriteLine();
            await largePersonGroupClient.DeleteAsync();
            Console.WriteLine($"Deleted the person group {LargePersonGroupId}.");
            Console.WriteLine();
        }
    }
}
