using Microsoft.AspNetCore.Http;

namespace growTests.TestsHelpers
{
    public class FakeFormFile : IFormFile
    {
        private readonly Stream _stream;

        public FakeFormFile(Stream stream, long length, string name, string fileName, string contentType = "image/jpeg")
        {
            _stream = stream;
            Length = length;
            Name = name;
            FileName = fileName;
            ContentType = contentType;
        }

        public string ContentType { get; }

        public string ContentDisposition => throw new NotImplementedException();

        public IHeaderDictionary Headers => throw new NotImplementedException();

        public long Length { get; }

        public string Name { get; }

        public string FileName { get; }

        public void CopyTo(Stream target)
        {
            _stream.CopyTo(target);
        }

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            return _stream.CopyToAsync(target, cancellationToken);
        }

        public Stream OpenReadStream()
        {
            return _stream;
        }
    }
}
