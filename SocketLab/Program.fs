module Program

open Suave
open Suave.Files
open Suave.Filters
open Suave.Logging
open Suave.Operators
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open System
open System.Net
open System.Text
open System.Threading

type broadcastMessage =
    | Subscribe of MailboxProcessor<Opcode * ByteSegment * bool>
    | Unsubscribe of MailboxProcessor<Opcode * ByteSegment * bool>
    | Broadcast of ByteSegment

let broadcastAgent =
    MailboxProcessor<broadcastMessage>.Start(fun inbox ->
        let rec loop subscribers =
            async {
                let! msg = inbox.Receive()
                match msg with
                | Subscribe(mailbox) -> do! loop (mailbox :: subscribers)
                | Unsubscribe(mailbox) -> do! loop (subscribers |> List.filter (fun x -> not (x = mailbox)))
                | Broadcast(data) ->
                    subscribers |> List.iter (fun x -> x.Post(Text, data, true))
                    let ms = sprintf "Broadcasting?? %A" data
                    Console.WriteLine ms
                    do! loop subscribers
            }
        loop List.empty)

let handleWebsocketConnection (ws : WebSocket) =
    fun _ ->
        let mutable loop = true

        let inbox =
            MailboxProcessor.Start(fun inbox ->
                async {
                    let mutable close = false
                    while not close do
                        let! op, data, fi = inbox.Receive()
                        let! _ = ws.send op data fi
                        close <- op = Close
                })
        broadcastAgent.Post(Subscribe(inbox))
        socket {
            while loop do
                let! m = ws.read()
                match m with
                | Text, data, true -> broadcastAgent.Post(Broadcast( ByteSegment data))
                | Ping, _, _ -> inbox.Post(Pong, ByteSegment [||], true)
                | Close, _, _ ->
                    inbox.Post(Close, ByteSegment [||], true)
                    loop <- false
                | _ -> ()
        }

let app : WebPart =
    choose [ path "/websocket" >=> handShake handleWebsocketConnection
             path "/logic.js" >=> file "logic.js"
             path "/" >=> file "index.html"
             ]

[<EntryPoint>]
let main _ =
    startWebServer { defaultConfig with logger = Targets.create Verbose [||] } app
    0