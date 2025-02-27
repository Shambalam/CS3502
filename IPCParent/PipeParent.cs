using System;
using System.IO.Pipes;
using System.Text;

public class PipeParent
{
    private static void Main()
    {
        using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("testPipe", PipeDirection.Out))
        {
            Console.WriteLine("Waiting for Client Connection...");
            pipeServer.WaitForConnection();

            // Message sent to client
            string message = "Successful Send from Parent HAHAHAHAHA!";
            byte[] messageByte = Encoding.UTF8.GetBytes(message);

            // Write byte message to pipe
            pipeServer.Write(messageByte, 0, messageByte.Length);
            pipeServer.Flush();  // Ensure all data is sent
            Console.WriteLine("Message sent to child");
        }  // Pipe is automatically closed when exiting 'using' block
    }
}
