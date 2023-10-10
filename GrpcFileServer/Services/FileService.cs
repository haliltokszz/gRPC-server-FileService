using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using static GrpcFileServer.FileService;

namespace GrpcFileServer.Services
{
    public class FileService: FileServiceBase
    {
        readonly IWebHostEnvironment _webHostEnvironment;
        public FileService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }
        public override async Task<Empty> FileUpLoad(IAsyncStreamReader<BytesContent> requestStream, ServerCallContext context)
        {
            string path = Path.Combine(_webHostEnvironment.WebRootPath, "files");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            FileStream fileStream = null;

            try
            {
                int count = 0;

                //Yüzdelik hesaplaması için 'chunkSize' değişkeni tanımlanıyor.
                decimal chunkSize = 0;

                while (await requestStream.MoveNext())
                {
                    //Stream ilk başladığında(ilk adımda) yapılması gereken öncelikli işlevler
                    if (count++ == 0)
                    {
                        string filePath = $"{path}/{requestStream.Current.Info.FileName}{requestStream.Current.Info.FileExtension}";
                        fileStream = new FileStream(filePath, FileMode.CreateNew);

                        //Gelecek dosya boyutu kadar alan tahsis ediliyor.
                        fileStream.SetLength(requestStream.Current.FileSize);
                    }

                    var buffer = requestStream.Current.Buffer.ToByteArray();

                    await fileStream.WriteAsync(buffer, 0, requestStream.Current.ReadedByte);

                    chunkSize += requestStream.Current.ReadedByte;
                    decimal uploadedRate = Math.Round((chunkSize * 100) / requestStream.Current.FileSize);
                    Console.WriteLine($"{uploadedRate}%");
                }
                Console.WriteLine("Uploaded!");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
            }
            finally { 
                if (fileStream != null) { 
                    await fileStream.DisposeAsync();
                    fileStream.Close();
                } 
            }
            return new Empty();
        }

        public override async Task FileDownLoad(FileInfo request, IServerStreamWriter<BytesContent> responseStream, ServerCallContext context)
        {
            string path = Path.Combine(_webHostEnvironment.WebRootPath, "files");

            string filePath = $"{path}/{request.FileName}{request.FileExtension}";
            try
            {
                bool isFileExist = File.Exists(filePath);
                if (!isFileExist) throw new FileNotFoundException(filePath);

                using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                byte[] buffer = new byte[2048];

                BytesContent content = new BytesContent
                {
                    FileSize = fileStream.Length,
                    Info = new FileInfo { FileName = request.FileName + "_gRPC_FileService", FileExtension = request.FileExtension },
                    ReadedByte = 0
                };

                while ((content.ReadedByte = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    //Okunan buffer'ın stream edilebilmesi için 'message.proto' dosyasındaki 'bytes' türüne dönüştürülüyor.
                    content.Buffer = ByteString.CopyFrom(buffer);
                    await responseStream.WriteAsync(content);
                }
                fileStream.Close();
            }catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public override Task<FilePath> GetFilePath(FileInfo request, ServerCallContext context)
        {
            string path = Path.Combine(_webHostEnvironment.WebRootPath, "files");
            string filePath = $"{path}/{request.FileName}{request.FileExtension}";
            bool isFileExist = File.Exists(filePath);

            return Task.FromResult(isFileExist ? 
                new FilePath() { FilePath_ = filePath }
                : throw new FileNotFoundException(filePath));
        }
    }
}
