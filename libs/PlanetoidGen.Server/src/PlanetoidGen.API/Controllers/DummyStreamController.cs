using Grpc.Core;

namespace PlanetoidGen.API.Controllers
{
    public class DummyStreamController : DummyStream.DummyStreamBase
    {
        private readonly ILogger<DummyStreamController> _logger;

        public DummyStreamController(ILogger<DummyStreamController> logger)
        {
            _logger = logger;
        }

        public override async Task SendStreamMessage(IAsyncStreamReader<StreamRequest> requestStream, IServerStreamWriter<StreamReply> responseStream, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            _logger.LogInformation($"Connection id: {httpContext.Connection.Id}");

            try
            {
                while (await requestStream.MoveNext())
                {
                    if (!string.IsNullOrEmpty(requestStream.Current.Message))
                    {
                        //var client = new MongoClient(
                        //    //"mongodb://admin:password@host.docker.internal:9096/?authSource=admin"
                        //    "mongodb://mongo.mongodb.svc.cluster.local:27017"
                        //);
                        //var database = client.GetDatabase("test");
                        //var collection = database.GetCollection<Book>("movies");
                        //collection.InsertOne(new Book { Id = "61a6058e6c43f32854e51f51", BookName = "test", Price = (decimal)54.93 });

                        //await responseStream.WriteAsync(new StreamReply { Message = collection.Find(b => b.BookName == "test").FirstOrDefault().BookName });

                        await responseStream.WriteAsync(new StreamReply { Message = $"Response #1 on connection {httpContext.Connection.Id}" });
                    }
                }
            }
            catch (IOException)
            {
                _logger.LogInformation($"Connection was aborted.");
            }
        }
    }

    //public class Book
    //{
    //    [BsonId]
    //    [BsonRepresentation(BsonType.ObjectId)]
    //    public string? Id { get; set; }

    //    [BsonElement("Name")]
    //    public string BookName { get; set; } = null!;

    //    public decimal Price { get; set; }

    //    public string Category { get; set; } = null!;

    //    public string Author { get; set; } = null!;
    //}
}
