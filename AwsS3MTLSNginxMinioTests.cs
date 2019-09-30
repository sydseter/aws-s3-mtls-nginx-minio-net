using Amazon.S3;
using Amazon;
using NUnit.Framework;
using System;
using System.Diagnostics;
using Amazon.S3.Model;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Amazon.Runtime;

namespace AwsS3MTLSNginxMinioTests
{
    public class MyHttpClientFactory : Amazon.Runtime.HttpClientFactory
    {
        public override HttpClient CreateHttpClient(IClientConfig clientConfig)
        {
            
            var httpMessageHandler = CreateClientHandler();
            //Adds the client certificate to the Http message handler
            httpMessageHandler.ClientCertificates.Add(LoadClientCertificateCredentials());
            if (clientConfig.MaxConnectionsPerServer.HasValue)
            {
                httpMessageHandler.MaxConnectionsPerServer = clientConfig.MaxConnectionsPerServer.Value;
            }
            httpMessageHandler.AllowAutoRedirect = clientConfig.AllowAutoRedirect;
            var proxy = clientConfig.GetWebProxy();
            if (proxy != null)
            {
                httpMessageHandler.Proxy = proxy;
            }
            
            if (httpMessageHandler.Proxy != null && clientConfig.ProxyCredentials != null)
            {
                httpMessageHandler.Proxy.Credentials = clientConfig.ProxyCredentials;
            }
            var httpClient = new HttpClient(httpMessageHandler);
            if (clientConfig.Timeout.HasValue)
            {
                httpClient.Timeout = clientConfig.Timeout.Value;
            }
            return httpClient;
        }
        protected virtual HttpClientHandler CreateClientHandler() =>
            new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; },
            };
        private X509Certificate2 LoadClientCertificateCredentials()
        {
            String clientCertToLoad = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Resources/certs/client.p12"));
            return new X509Certificate2(clientCertToLoad, "root");
        }

    }

    public class AwsS3MTLSNginxMinioTest
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void TestCanPutFileToS3Bucket()
        {
            Debug.WriteLine("testing that I can write a line");
            var accessKey = Environment.GetEnvironmentVariable("AccessKey");
            Assert.IsNotNull(accessKey, "AccessKey: {0}", accessKey, "Please set the Minio AccessKey as an environment variable before running the tests.");
            var secretKey = Environment.GetEnvironmentVariable("SecretKey");
            Assert.IsNotNull(accessKey, "SecretKey: {0}", secretKey, "Please set the Minio SecretKey as an environment variable before running the tests.");
            var config = new AmazonS3Config
            {
                //RegionEndpoint = RegionEndpoint.USEast1, // MUST set this before setting ServiceURL and it should match the `MINIO_REGION` enviroment variable.
                ServiceURL = "https://localhost:8092", // replace http://localhost:8092 with URL of your MinIO server
                ForcePathStyle = true, // MUST be true to work correctly with MinIO server
                HttpClientFactory = new MyHttpClientFactory()
            };
            var amazonS3Client = new AmazonS3Client(accessKey, secretKey, config);
            String fileToUpload = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Resources/healthTreatment.json"));
            var putRequest = new PutObjectRequest
            {
                BucketName = "test",
                Key = "healthTreatment.json",
                FilePath = fileToUpload,
                ContentType = "text/plain"
            };
            WritingAnObjectAsync(amazonS3Client, putRequest).Wait();

        }

        static async Task WritingAnObjectAsync(AmazonS3Client amazonS3Client, PutObjectRequest putRequest)
        {

            PutObjectResponse response = await amazonS3Client.PutObjectAsync(putRequest);
            Assert.AreEqual("OK", response.HttpStatusCode.ToString().Trim());
        }

        [Test]
        public void TestCanGetFileFromS3Bucket()
        {
            Debug.WriteLine("testing that I can write a line");
            var accessKey = Environment.GetEnvironmentVariable("AccessKey");
            Assert.IsNotNull(accessKey, "AccessKey: {0}", accessKey, "Please set the Minio AccessKey as an environment variable before running the tests.");
            var secretKey = Environment.GetEnvironmentVariable("SecretKey");
            Assert.IsNotNull(accessKey, "SecretKey: {0}", secretKey, "Please set the Minio SecretKey as an environment variable before running the tests.");
            var config = new AmazonS3Config
            {
                //RegionEndpoint = RegionEndpoint.USEast1, // MUST set this before setting ServiceURL and it should match the `MINIO_REGION` enviroment variable.
                ServiceURL = "https://localhost:8092", // replace http://localhost:8092 with URL of your MinIO server
                ForcePathStyle = true, // MUST be true to work correctly with MinIO server
                HttpClientFactory = new MyHttpClientFactory()
            };
            var amazonS3Client = new AmazonS3Client(accessKey, secretKey, config);

            var getRequest = new GetObjectRequest
            {
                BucketName = "test",
                Key = "healthTreatment.json"
            };
            ReadingAnObjectAsync(amazonS3Client, getRequest).Wait();
        }

        static async Task ReadingAnObjectAsync(AmazonS3Client amazonS3Client, GetObjectRequest getRequest)
        {
            GetObjectResponse response = await amazonS3Client.GetObjectAsync(getRequest);
            Assert.AreEqual("OK", response.HttpStatusCode.ToString().Trim());
        }
    }
}

