"use strict";

var http = require("http");
var fs = require("fs");
var path = require("path");

var publicFiles = {
    "/": ["index.html", "text/html; charset=utf-8"],
    "/index.html": ["index.html", "text/html; charset=utf-8"],
    "/home.css": ["home.css", "text/css; charset=utf-8"],
    "/register.html": ["register.html", "text/html; charset=utf-8"],
    "/register.css": ["register.css", "text/css; charset=utf-8"],
    "/register.js": ["register.js", "application/javascript; charset=utf-8"],
    "/dashboard.html": ["dashboard.html", "text/html; charset=utf-8"],
    "/dashboard.css": ["dashboard.css", "text/css; charset=utf-8"],
    "/dashboard.js": ["dashboard.js", "application/javascript; charset=utf-8"],
    "/config.js": ["config.js", "application/javascript; charset=utf-8"]
};

function sendJson(response, statusCode, data) {
    var body = Buffer.from(JSON.stringify(data));
    response.writeHead(statusCode, {
        "Content-Type": "application/json; charset=utf-8",
        "Content-Length": body.length,
        "Cache-Control": "no-store",
        "X-Content-Type-Options": "nosniff"
    });
    response.end(body);
}

function sendUnavailable(response) {
    sendJson(response, 503, {
        ok: false,
        message: "The account and live game API is not deployed on this website yet."
    });
}

function handleRequest(request, response) {
    var requestPath = request.url.split("?")[0];

    response.setHeader("X-Content-Type-Options", "nosniff");
    response.setHeader("X-Frame-Options", "DENY");
    response.setHeader("Referrer-Policy", "strict-origin-when-cross-origin");

    if (requestPath === "/healthz" && (request.method === "GET" || request.method === "HEAD")) {
        var health = Buffer.from(JSON.stringify({ ok: true }));
        response.writeHead(200, {
            "Content-Type": "application/json; charset=utf-8",
            "Content-Length": health.length,
            "Cache-Control": "no-store"
        });
        response.end(request.method === "HEAD" ? undefined : health);
        return;
    }

    if (requestPath === "/api" || requestPath.indexOf("/api/") === 0) {
        sendUnavailable(response);
        return;
    }

    if (requestPath.indexOf("/client-updates/") === 0 ||
        requestPath.indexOf("/downloads/") === 0) {
        response.writeHead(404, { "Cache-Control": "no-store" });
        response.end("Not found.");
        return;
    }

    if (request.method !== "GET" && request.method !== "HEAD") {
        response.writeHead(405, { Allow: "GET, HEAD", "Cache-Control": "no-store" });
        response.end("Method not allowed.");
        return;
    }

    var publicFile = publicFiles[requestPath];
    if (!publicFile) {
        response.writeHead(404, { "Cache-Control": "no-store" });
        response.end("Not found.");
        return;
    }

    var filePath = path.join(__dirname, publicFile[0]);
    fs.readFile(filePath, function (error, contents) {
        if (error) {
            console.error("Unable to read public website file:", error.message);
            response.writeHead(500, { "Cache-Control": "no-store" });
            response.end("Website file unavailable.");
            return;
        }

        response.writeHead(200, {
            "Content-Type": publicFile[1],
            "Content-Length": contents.length,
            "Cache-Control": publicFile[0].endsWith(".html") ? "no-store" : "public, max-age=300"
        });
        response.end(request.method === "HEAD" ? undefined : contents);
    });
}

var port = Number(process.env.PORT) || 8080;
var server = http.createServer(handleRequest);

server.on("error", function (error) {
    console.error("Website server failed to start:", error.message);
    process.exitCode = 1;
});

server.listen(port, "0.0.0.0", function () {
    console.log("Wonderland website listening on port " + port);
});
