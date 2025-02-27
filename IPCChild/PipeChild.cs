using System;
using System.IO.Pipes;
using System.Text;

public class PipeChild
{
    private static void Main()
    {
        using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "testPipe", PipeDirection.In))
        {
            Console.WriteLine("Connecting to pipe...");
            pipeClient.Connect();
            Console.WriteLine("Connected to pipe!");

            byte[] buffer = new byte[256];
            int bytesRead = pipeClient.Read(buffer, 0, buffer.Length);

            // Convert bytes back to string
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Child received message: {message}");
        }  // Pipe is automatically closed when exiting 'using' block
    }
}
