// ABOUT THE RUSTWEB CONSOLE SOCKET
// -------------------------------------------------------------------------------------------
// To be able to connect to the console socket, the user needs to be signed in through Steam
// on RustWeb first.
//
// The console socket emits, once authenticated, log events in the form of a JSON formatted
// object with the following properties:
//
// - type: The log type, one of "assert", "log", "warning", "error", "exception"
// - message: The log message as a string
// - stack: The stacktrace, if any, as a string
//
// To execute a command, just send it to the socket as a string. That's pretty much it.

// Some references to reduce lookups
var $log = $("#log");
var $input = $("#input");
var $form = $('#form');
var $wrap = $('#wrap');
var $bumper = $('#bumper');
var $window = $(window);
var connected = false;
var ws;

// Logs a message to the console view
function log(type, msg, stack) {
    var elem = $('<div />');
    elem.addClass(type);
    elem.text(msg);
    $wrap.append(elem);
    if ($input.is(":focus"))
        $bumper[0].scrollIntoView();
    if (stack) {
        log("stack", stack);
    }
}

// Connects to the console socket
function connect() {
    if (connected)
        return;
    log("debug", "Connecting to " + document.location.hostname + ":" + document.location.port + " ...");
    try {
        ws = new WebSocket("ws://" + document.location.hostname + ":" + document.location.port + "/cs");
        ws.onopen = function () {
            connected = true;
            log("debug", "Connected");
            $input.focus();
        }
        ws.onmessage = function (e) {
            var data = JSON.parse(e.data);
            if (!data.type)
                data.type = "log";
            log(data.type, data.message, data.stack);
        }
        ws.onclose = function (evt) {
            connected = false;
            log("debug", "Disconnected: " + evt.code + ", " + (evt.reason ? evt.reason : "(no reason provided)"));
            var elem = $('<a />');
            elem.addClass("console");
            elem.prop("href", "javascript:void(0)");
            elem.text("Click to reconnect");
            elem.click(function (evt) {
                evt.preventDefault();
                connect();
                $(this).remove();
                return false;
            });
            $wrap.append(elem);
            $bumper[0].scrollIntoView();
        }
    } catch (err) {
        log("error", err.message);
    }
}

// Initializes the app
$(document).ready(function () {
    $form.submit(function (evt) {
        evt.preventDefault();
        if (!connected)
            return false;
        var cmd = $input.val();
        $input.val('');
        $input.select();
        $input.focus();
        log("debug", "> " + cmd);
        try {
            ws.send(cmd);
        } catch (err) {
            log("error", err.message);
        }
        return false;
    });
    $input.focus(function () {
        $bumper[0].scrollIntoView();
    });
    $window.resize(onResize);

    onResize();
    connect();
});

// Handles resizing the window
function onResize() {
    $log.css({
        height: ($window.innerHeight()-28)+"px"
    });
}
