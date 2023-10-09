using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.IO;
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
                    //Stream ilk başladığında(ilk adımda) yapılması gereken öncelikli işlevler gerçekleştiriliyor.
                    if (count++ == 0)
                    {
                        //Stream'de gelen Info nesnesinin FileName özelliğiyle hedef dosyanın adı belirleniyor.
                        string filePath = $"{path}/{requestStream.Current.Info.FileName}{requestStream.Current.Info.FileExtension}";
                        fileStream = new FileStream(filePath, FileMode.CreateNew);

                        //Gelecek dosya boyutu kadar alan tahsis ediliyor. Bu işlem zorunlu değildir
                        //lakin süreçte farklı bir program tarafından diskin doldurulup, işimize engel olmasının önüne geçiyoruz.
                        fileStream.SetLength(requestStream.Current.FileSize);
                    }

                    //Buffer, akışta gelen her bir parçanın ta kendisidir. Chunk olarak isimlendirilir.
                    var buffer = requestStream.Current.Buffer.ToByteArray();

                    //Akışta gelen chunk'ları hedef FileStream nesnesine yazdırıyoruz.
                    //Burada, ikinci parametrede ki '0' değeri ile buffer'dan kaçıncı byte'dan itibaren okunacağı ve
                    //yazdırılacağı bildirilmektedir.
                    await fileStream.WriteAsync(buffer, 0, requestStream.Current.ReadedByte);

                    //Akışın yüzdelik olarak ne kadarının aktarıldığı hesaplanıyor.
                    //Formülasyon olarak;
                    //Okunan parça sayısı(ReadedByte), chunkSize değişkeninde toplanıyor ve 100 ile çarpılıp
                    //sonuç toplam boyuta bölünüyor. Nihai sonuç ise yakın olan tam sayıya yuvarlanıyor ve
                    //yüzdelik olarak ne kadarlık aktarım gerçekleştirildiği hesaplanmış oluyor.
                    chunkSize += requestStream.Current.ReadedByte;
                    decimal uploadedRate = Math.Round((chunkSize * 100) / requestStream.Current.FileSize);
                    Console.WriteLine($"{uploadedRate}%");
                }
                Console.WriteLine("Uploaded!");

            }
            catch (Exception ex)
            {
                //Client'ta stream 'CompleteAsync' edildiği vakit burada olası hata meydana gelebilmektedir.
                //Dolayısıyla tüm bu süreci try catch ile kontrol ediyoruz.
                Console.WriteLine("Exception: " + ex.ToString());
            }
            await fileStream.DisposeAsync();
            fileStream.Close();
            return new Empty();
        }

        public override async Task FileDownLoad(FileInfo request, IServerStreamWriter<BytesContent> responseStream, ServerCallContext context)
        {
            string path = Path.Combine(_webHostEnvironment.WebRootPath, "files");

            string filePath = $"{path}/{request.FileName}{request.FileExtension}";
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
