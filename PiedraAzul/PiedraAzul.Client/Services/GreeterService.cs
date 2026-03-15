using Shared.Grpc;

namespace PiedraAzul.Client.Services
{
    public class GreeterService
    {
        private readonly Greeter.GreeterClient _client;

        public GreeterService(Greeter.GreeterClient client)
        {
            _client = client;
        }

        public async Task<HelloReply> SayHello(string name)
        {
            var response = await _client.SayHelloAsync(
                new HelloRequest
                {
                    Name = name
                });

            return response;
        }
    }
}
