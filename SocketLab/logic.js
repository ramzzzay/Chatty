let output;
let nick;
let msgs;

let init =
    () => {
        output = document.getElementById("chat");
        msgs = document.getElementById("message");
        nick = document.getElementById("nick");

        websocket = new WebSocket("ws://localhost:8080/websocket");
        websocket.onopen = e => writeToScreen("CONNECTED", 1);
        websocket.onclose = e => writeToScreen("DISCONNECTED", 1);
        websocket.onmessage = e => writeToScreen(e.data);
        websocket.onerror = e => writeToScreen(e.data, 1);
    };

function doSend()
{
    websocket.send(`${nick.value}: ${msgs.value}`);
}

function writeToScreen(message, type)
{
    let pre = document.createElement("p");
    pre.style.wordWrap = "break-word";
    let msgToSend = type ? `<span style="color: lightcoral;">${message}</span>` : `<span style="color: lightgreen;">${message}</span>`;
    pre.innerHTML = msgToSend;
    output.appendChild(pre);
}

window.addEventListener("load", init, false);
