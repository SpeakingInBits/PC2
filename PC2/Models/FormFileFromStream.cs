namespace PC2.Models
{
    /// <summary>
    /// Implementation of IFormFile that wraps a Stream
    /// Used for creating IFormFile from resized image streams
    /// </summary>
    public class FormFileFromStream : IFormFile
    {
        private readonly Stream _stream;
        private readonly string _name;
        private readonly string _contentType;

        public FormFileFromStream(Stream stream, string name, string contentType)
        {
            _stream = stream;
            _name = name;
            _contentType = contentType;
        }

        public string ContentType => _contentType;
        public string ContentDisposition => $"form-data; name=\"{Name}\"; filename=\"{FileName}\"";
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length => _stream.Length;
        public string Name => _name;
        public string FileName => _name;

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
            _stream.Position = 0;
            return _stream;
        }
    }
}