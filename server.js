const http = require('http');

const requestListener = function (req, res) {
  let clientIp = req.headers['x-forwarded-for'] || req.socket.remoteAddress || null;
  let data = '';

  req.on('data', chunk => {
    data += chunk;
  });

  req.on('end', () => {
    console.log(`${req.method} from ${clientIp}: '${data}'`);

    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'POST, GET, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'access-control-allow-headers, content-type');

    let simulateError = Math.floor(Math.random() * 100) > 70;

    if (simulateError) {
      res.writeHead(400);
      res.end('Bad request');
    } else {
      res.writeHead(200);
      res.end('OK');
    }
  });
}

const server = http.createServer(requestListener);
const host = '127.0.0.1';
const port = 8080;

server.listen(port, host, () => {
  console.log(`Server is running on http://${host}:${port}`);
});