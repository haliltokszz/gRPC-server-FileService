# .NET Core 7 - Grpc Server - FileService
**Readme.md**

**Introduction**

This is a gRPC server for file management, written in .NET Core 7. It provides three methods:

* `FileDownLoad()`: Downloads a file from the server.
* `FileUpLoad()`: Uploads a file to the server.
* `GetFilePath()`: Gets the path of a file on the server.

**Prerequisites**

* .NET Core 7
* Docker

**Building and running the server**

To build and run the server, run the following commands:

```
docker build -t grpc-file-server .
docker run -p 5000:5000 grpc-file-server
```

**Using the server**

To use the server, you need to install a gRPC client. Once you have installed a gRPC client, you can use it to call the server's methods.

**FileDownLoad()**

To download a file from the server, call the `FileDownLoad()` method with the file name as the parameter. The method will return a stream of bytes, which you can write to a file on your local machine.

**FileUpLoad()**

To upload a file to the server, call the `FileUpLoad()` method with a stream of bytes as the parameter. The method will save the bytes to a file on the server.

**GetFilePath()**

To get the path of a file on the server, call the `GetFilePath()` method with the file name as the parameter. The method will return the path of the file on the server.

**Examples**

The following examples show how to use the gRPC server to download a file, upload a file, and get the path of a file.

**Downloading a file**

```
// Create a gRPC client.
const client = new FileServiceClient('localhost:5000');

// Download a file.
const request = new FileInfo();
request.fileName = 'my-file.txt';

const response = await client.FileDownLoad(request);

// Write the bytes to a file on the local machine.
const fileStream = await response.stream.pipe(fs.createWriteStream('./my-file.txt'));
await fileStream.close();
```

**Uploading a file**

```
// Create a gRPC client.
const client = new FileServiceClient('localhost:5000');

// Upload a file.
const request = new FileInfo();
request.fileName = 'my-file.txt';

const fileStream = fs.createReadStream('./my-file.txt');

const response = await client.FileUpLoad(fileStream);
```

**Getting the path of a file**

```
// Create a gRPC client.
const client = new FileServiceClient('localhost:5000');

// Get the path of a file.
const request = new FileInfo();
request.fileName = 'my-file.txt';

const response = await client.GetFilePath(request);

// The path of the file on the server.
const filePath = response.filePath;
```

**Conclusion**

This is a simple gRPC server for file management. It can be used to download, upload, and get the path of files on a server.
